using HtmlAgilityPack;
using MolenApplicatie.Server.Models;
using System.Net;
using System.Net.Http.Headers;

namespace MolenApplicatie.Server.Services
{
    public class MillDatabaseRemoteImportService
    {
        private const string SearchBaseUrl = "https://milldatabase.org/search/international/detail";
        private const string CsvDownloadUrl = "https://milldatabase.org/search/download";
        private const string ImportLanguage = "en";

        private static readonly string[] MillTypes =
        [
            "hollow post mill",
            "smock mill",
            "tower mill",
            "combined wind and watermill",
            "composite mill",
            "inverted windmill",
            "paltrok mill",
            "post mill"
        ];

        private static readonly string[] ExistingConditionTypes =
        [
            "not functional",
            "in restoration",
            "functional",
            "dismantled",
            "no technique",
            "remains",
            "stored",
            "technique",
            "under construction"
        ];

        private static readonly string[] ExistingSymbols =
        [
            "windmill",
            "windmill_tower",
            "windmill_turn",
            "windmill_work",
            "windmill_nophoto"
        ];

        private static readonly string[] RuinSymbols =
        [
            "windmill_ruin"
        ];

        private static readonly string[] DisappearedSymbols =
        [
            "windmill_gone"
        ];

        private static readonly MillDatabaseImportDefinition[] ImportDefinitions =
        [
            new(                "bestaande molens",                ExistingConditionTypes,                ExistingSymbols,                null),
            new(                "restanten",                ExistingConditionTypes,                RuinSymbols,                "windmill_ruin"),
            new(                "verdwenen molens",                [],                DisappearedSymbols,                "windmill_gone")
        ];

        private readonly HttpClient _httpClient;
        private readonly MillDatabaseCsvImportService _csvImportService;

        public MillDatabaseRemoteImportService(HttpClient httpClient, MillDatabaseCsvImportService csvImportService)
        {
            _httpClient = httpClient;
            _csvImportService = csvImportService;
        }

        public Task<MillDatabaseImportResult> ImportAsync(CancellationToken token = default)
        => ImportAsync(null, token);

        public async Task<MillDatabaseImportResult> ImportAsync(ProgressService? progressService, CancellationToken token = default)
        {
            var combinedResult = new MillDatabaseImportResult();
            var processedExternalReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var importIndex = 0; importIndex < ImportDefinitions.Length; importIndex++)
            {
                token.ThrowIfCancellationRequested();

                var definition = ImportDefinitions[importIndex];
                var stepNumber = importIndex + 1;

                progressService?
                    .SetStepNumber(stepNumber)
                    .SetProgressMessage(
                        $"[{stepNumber}/{ImportDefinitions.Length}] " +
                        $"Importing {definition.Name}...");

                var searchUri = BuildSearchUri(definition);
                var searchPageHtml = await DownloadSearchPageHtmlAsync(searchUri, token);
                token.ThrowIfCancellationRequested();

                var authenticityToken = GetAuthenticityToken(searchPageHtml);

                progressService?.SetProgressMessage($"Downloading CSV for {definition.Name}...", logToConsole: false);

                using var csvResponse = await DownloadCsvAsync(searchUri, authenticityToken, definition, token);

                await using var csvStream = await csvResponse.Content.ReadAsStreamAsync(token);

                progressService?.SetProgressMessage($"Processing {definition.Name}...", logToConsole: false);

                var importResult = await _csvImportService.ImportAsync(
                    csvStream,
                    definition.SourceSymbol,
                    processedExternalReferences,
                    progressService,
                    token);

                var csvUrl = csvResponse.RequestMessage?.RequestUri?.AbsoluteUri ?? CsvDownloadUrl;
                MergeResult(combinedResult, importResult, definition.Name, searchUri.AbsoluteUri, csvUrl);

                progressService?.SetProgressMessage(
                    $"[{stepNumber}/{ImportDefinitions.Length}] Completed {definition.Name}: " +
                    $"{importResult.ImportedRows:N0} imported, " +
                    $"{importResult.AddedMolens:N0} added, " +
                    $"{importResult.UpdatedMolens:N0} updated, " +
                    $"{importResult.SkippedExistingMolens:N0} skipped.");
            }

            combinedResult.SearchUrl = combinedResult.SearchUrls.FirstOrDefault();
            combinedResult.CsvUrl = combinedResult.CsvUrls.FirstOrDefault();

            return combinedResult;
        }

        private static Uri BuildSearchUri(MillDatabaseImportDefinition definition)
        {
            var queryParameters = new List<KeyValuePair<string, string>>();

            AddQueryParameter(queryParameters, "lang", ImportLanguage);
            AddQueryParameter(queryParameters, "utf8", "✓");

            AddQueryParameters(queryParameters, "mill_type[]", MillTypes);

            AddQueryParameter(queryParameters, "id", string.Empty);

            AddQueryParameters(
                queryParameters,
                "condition_type[]",
                definition.ConditionTypes);

            AddQueryParameter(queryParameters, "external_number", string.Empty);
            AddQueryParameter(queryParameters, "internal_number", string.Empty);
            AddQueryParameter(queryParameters, "name", string.Empty);

            AddQueryParameter(queryParameters, "symboleToggler", "complex");
            AddQueryParameters(
                queryParameters,
                "symbol[]",
                definition.Symbols);

            AddQueryParameter(queryParameters, "commit", "start search");

            return AddQueryParameters(new Uri(SearchBaseUrl), queryParameters);
        }

        private async Task<string> DownloadSearchPageHtmlAsync(Uri searchUri, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, searchUri);
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(ImportLanguage));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    "De zoekpagina van Mill Database kon niet worden opgehaald. " +
                    $"Statuscode: {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return await response.Content.ReadAsStringAsync(token);
        }

        private async Task<HttpResponseMessage> DownloadCsvAsync(
            Uri searchUri,
            string authenticityToken,
            MillDatabaseImportDefinition definition,
            CancellationToken token)
        {
            var formValues = BuildCsvDownloadFormValues(authenticityToken, definition);
            var csvDownloadUri = AddQueryParameters(new Uri(CsvDownloadUrl), [new KeyValuePair<string, string>("lang", ImportLanguage)]);

            using var request = new HttpRequestMessage(HttpMethod.Post, csvDownloadUri) { Content = new FormUrlEncodedContent(formValues) };

            request.Headers.Referrer = searchUri;
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(ImportLanguage));
            request.Headers.TryAddWithoutValidation("Origin", "https://milldatabase.org");
            request.Headers.TryAddWithoutValidation("X-CSRF-Token", authenticityToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/csv"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream", 0.9));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.8));

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                response.Dispose();

                throw new HttpRequestException(
                    $"De CSV-download van Mill Database voor {definition.Name} " +
                    $"is mislukt. Statuscode: {(int)statusCode} ({statusCode}).");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;

            if (mediaType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true)
            {
                response.Dispose();

                throw new InvalidDataException(
                    $"De CSV POST-request voor {definition.Name} gaf een " +
                    "HTML-pagina terug in plaats van een CSV-bestand.");
            }

            return response;
        }

        private static List<KeyValuePair<string, string>> BuildCsvDownloadFormValues(string authenticityToken, MillDatabaseImportDefinition definition)
        {
            var formValues = new List<KeyValuePair<string, string>>();

            AddFormValue(formValues, "authenticity_token", authenticityToken);
            AddFormValue(formValues, "format", "csv");
            AddFormValue(formValues, "sort", "name");
            AddFormValue(formValues, "lang", ImportLanguage);

            AddFormValues(formValues, "where[condition_type_en][]", definition.ConditionTypes);
            AddFormValues(formValues, "where[logo_detail][]", definition.Symbols);
            AddFormValues(formValues, "where[mill_type_en][]", MillTypes);
            return formValues;
        }

        private static void MergeResult(MillDatabaseImportResult target, MillDatabaseImportResult source, string importName, string searchUrl, string csvUrl)
        {
            target.TotalRows += source.TotalRows;
            target.ImportedRows += source.ImportedRows;
            target.AddedMolens += source.AddedMolens;
            target.UpdatedMolens += source.UpdatedMolens;
            target.SkippedExistingMolens += source.SkippedExistingMolens;
            target.SkippedInvalidRows += source.SkippedInvalidRows;
            target.SearchUrls.Add(searchUrl);
            target.CsvUrls.Add(csvUrl);
            target.CompletedImports.Add(importName);

            foreach (var stateCount in source.ToestandCounts)
            {
                target.ToestandCounts[stateCount.Key] = target.ToestandCounts.GetValueOrDefault(stateCount.Key) + stateCount.Value;
            }

            foreach (var warning in source.Warnings)
            {
                if (target.Warnings.Count >= 100)
                    break;

                target.Warnings.Add($"{importName}: {warning}");
            }
        }

        private static string GetAuthenticityToken(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var token = document.DocumentNode.SelectSingleNode("//input[@name='authenticity_token']")?.GetAttributeValue("value", null);
            token ??= document.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']")?.GetAttributeValue("content", null);

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidDataException(
                    "De zoekpagina is opgehaald, maar de authenticity_token " +
                    "voor de CSV POST-request kon niet worden gevonden.");
            }

            return WebUtility.HtmlDecode(token.Trim());
        }

        private static Uri AddQueryParameters(Uri uri, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var currentQuery = uri.Query.TrimStart('?');
            var addedQuery = string.Join(
                "&",
                parameters.Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}=" +
                    Uri.EscapeDataString(parameter.Value)));

            var builder = new UriBuilder(uri)
            {
                Query = string.IsNullOrWhiteSpace(currentQuery)
                    ? addedQuery
                    : string.IsNullOrWhiteSpace(addedQuery)
                        ? currentQuery
                        : currentQuery + "&" + addedQuery
            };

            return builder.Uri;
        }

        private static void AddQueryParameter(List<KeyValuePair<string, string>> queryParameters, string name, string value)
            => queryParameters.Add(new(name, value));

        private static void AddQueryParameters(List<KeyValuePair<string, string>> queryParameters, string name, IEnumerable<string> values)
        {
            foreach (var value in values) AddQueryParameter(queryParameters, name, value);
        }

        private static void AddFormValue(List<KeyValuePair<string, string>> formValues, string name, string value)
             => formValues.Add(new(name, value));

        private static void AddFormValues(List<KeyValuePair<string, string>> formValues, string name, IEnumerable<string> values)
        {
            foreach (var value in values) AddFormValue(formValues, name, value);
        }

        private sealed record MillDatabaseImportDefinition(string Name, IReadOnlyList<string> ConditionTypes, IReadOnlyList<string> Symbols, string? SourceSymbol);
    }
}