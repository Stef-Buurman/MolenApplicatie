using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Services.Database;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MolenApplicatie.Server.Services
{
    public class MillDatabaseCsvImportService
    {
        private const string SourceReferencePrefix = "MDB-";
        private const int BatchSize = 250;

        private readonly MolenDbContext _dbContext;
        private readonly DBMolenDataService _dbMolenDataService;

        public MillDatabaseCsvImportService(MolenDbContext dbContext, DBMolenDataService dbMolenDataService)
        {
            _dbContext = dbContext;
            _dbMolenDataService = dbMolenDataService;
        }

        public Task<MillDatabaseImportResult> ImportAsync(Stream csvStream, CancellationToken token = default)
            => ImportAsync(csvStream, null, token);


        public Task<MillDatabaseImportResult> ImportAsync(
            Stream csvStream,
            string? sourceSymbol,
            CancellationToken token = default)
            => ImportAsync(
                csvStream,
                sourceSymbol,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                token);

        public Task<MillDatabaseImportResult> ImportAsync(
            Stream csvStream,
            string? sourceSymbol,
            ISet<string> processedExternalReferences,
            CancellationToken token = default)
            => ImportAsync(
                csvStream,
                sourceSymbol,
                processedExternalReferences,
                null,
                token);

        public async Task<MillDatabaseImportResult> ImportAsync(
            Stream csvStream,
            string? sourceSymbol,
            ISet<string> processedExternalReferences,
            ProgressService? progressService,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(processedExternalReferences);
            var result = new MillDatabaseImportResult();
            progressService?.SetProgressMessage("Reading rows from the downloaded CSV...", logToConsole: false);
            var rows = ReadRows(csvStream, result, token);

            if (rows.Count == 0)
            {
                progressService?.SetProgressMessage("The downloaded CSV did not contain any data rows.", logToConsole: false);
                return result;
            }

            progressService?
                .SetTotalAmount(rows.Count)
                .SetProgressMessage(
                    $"CSV contains {rows.Count:N0} rows. Loading existing mill references...",
                    logToConsole: false);

            var existingMolens = await _dbContext.MolenData
                .AsNoTracking()
                .Select(molen => new ExistingMolenReference
                {
                    Id = molen.Id,
                    MolenTBNId = molen.MolenTBNId,
                    TenBruggeNr = molen.Ten_Brugge_Nr,
                    Details = molen.Bijzonderheden
                })
                .ToListAsync(token);

            var existingMolensByReference = existingMolens
                .Where(molen => !string.IsNullOrWhiteSpace(molen.TenBruggeNr))
                .GroupBy(
                    molen => NormalizeReference(molen.TenBruggeNr),
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            var existingMolensByExternalReference = existingMolens
                .SelectMany(molen => GetExternalReferenceAliases(molen)
                    .Select(reference =>
                        new KeyValuePair<string, ExistingMolenReference>(
                            reference,
                            molen
                        )))
                .GroupBy(
                    pair => pair.Key,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(pair => IsMillDatabaseReference(
                            pair.Value.TenBruggeNr) ? 1 : 0)
                        .First()
                        .Value,
                    StringComparer.OrdinalIgnoreCase
                );

            var processedSourceReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var molensToImport = new Dictionary<string, MolenData>(StringComparer.OrdinalIgnoreCase);

            progressService?.SetProgressMessage("Existing mill references loaded. Processing CSV rows...", logToConsole: false);

            var csvRowNumber = 1;
            var processedRowCount = 0;
            var lastReportedRowPercentage = -1;

            foreach (var row in rows)
            {
                csvRowNumber++;
                processedRowCount++;
                token.ThrowIfCancellationRequested();
                ReportProgress(progressService, processedRowCount, rows.Count, ref lastReportedRowPercentage);

                var sourceId = Get(row, "id", "bron_id", "milldatabase_id");
                var name = CleanText(Get(row, "name", "naam"));
                var latitudeValue = Get(row, "lat", "latitude", "breedtegraad");
                var longitudeValue = Get(row, "long", "longitude", "lengtegraad");

                if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(name)
                    || !TryParseCoordinate(
                        latitudeValue,
                        out var latitude,
                        out var correctedLatitude
                    )
                    || !TryParseCoordinate(
                        longitudeValue,
                        out var longitude,
                        out var correctedLongitude
                    )
                    || latitude is < -90 or > 90
                    || longitude is < -180 or > 180)
                {
                    result.SkippedInvalidRows++;
                    AddWarning(result, $"CSV-regel {csvRowNumber}: ongeldige id, naam of coördinaten.");
                    continue;
                }

                if (correctedLatitude || correctedLongitude) AddWarning(result, $"CSV-regel {csvRowNumber}: typfout in coördinaten automatisch hersteld.");
                sourceId = sourceId.Trim();

                var sourceReference = SourceReferencePrefix + sourceId;
                var normalizedSourceReference = NormalizeReference(sourceReference);

                if (!processedSourceReferences.Add(normalizedSourceReference))
                {
                    AddWarning(result, $"CSV-regel {csvRowNumber}: dubbele Mill Database-referentie " + $"{sourceReference} in hetzelfde bestand overgeslagen.");
                    result.SkippedExistingMolens++;
                    continue;
                }

                existingMolensByReference.TryGetValue(normalizedSourceReference, out var existingMolenBySourceReference);
                var normalizedExternalReferences = GetNormalizedExternalReferences(row);
                var duplicateProcessedExternalReference = normalizedExternalReferences
                    .FirstOrDefault(processedExternalReferences.Contains);

                if (!string.IsNullOrWhiteSpace(duplicateProcessedExternalReference))
                {
                    AddWarning(
                        result,
                        $"CSV-regel {csvRowNumber}: externe TBN-referentie " +
                        $"{duplicateProcessedExternalReference} is al in deze import " +
                        "verwerkt; de dubbele rij is overgeslagen."
                    );

                    result.SkippedExistingMolens++;
                    continue;
                }

                var existingMolenByExternalReference = normalizedExternalReferences
                    .Select(reference =>
                        existingMolensByExternalReference.GetValueOrDefault(reference))
                    .FirstOrDefault(molen => molen != null);

                if (existingMolenByExternalReference != null && (existingMolenBySourceReference == null || existingMolenBySourceReference.Id != existingMolenByExternalReference.Id))
                {
                    foreach (var reference in normalizedExternalReferences) processedExternalReferences.Add(reference);

                    var externalReference = normalizedExternalReferences.First(existingMolensByExternalReference.ContainsKey);
                    var oldMdbDuplicateMessage = existingMolenBySourceReference == null
                        ? string.Empty
                        : $" De oudere dubbele invoer " +
                            $"{existingMolenBySourceReference.TenBruggeNr} bestaat " +
                            "al in de database en moet eenmalig worden verwijderd.";

                    AddWarning(
                        result,
                        $"CSV-regel {csvRowNumber}: {sourceReference} niet " +
                        $"geïmporteerd, omdat externe TBN-referentie " +
                        $"{externalReference} al bestaat als " +
                        $"{existingMolenByExternalReference.TenBruggeNr}." +
                        oldMdbDuplicateMessage
                    );

                    result.SkippedExistingMolens++;
                    continue;
                }

                foreach (var reference in normalizedExternalReferences) processedExternalReferences.Add(reference);

                var molen = CreateMolen(row, sourceId, sourceReference, name, latitude, longitude, sourceSymbol);

                if (existingMolenBySourceReference != null)
                {
                    molen.Id = existingMolenBySourceReference.Id;
                    molen.MolenTBNId = existingMolenBySourceReference.MolenTBNId;
                    molen.MolenTBN = null!;
                    result.UpdatedMolens++;
                }
                else
                {
                    result.AddedMolens++;
                }

                result.ImportedRows++;
                result.ToestandCounts[molen.Toestand ?? MolenToestand.Bestaande] = result.ToestandCounts.GetValueOrDefault(molen.Toestand ?? MolenToestand.Bestaande) + 1;

                molensToImport.Add(normalizedSourceReference, molen
                );

            }

            var batches = molensToImport.Values.Chunk(BatchSize).ToList();

            if (batches.Count == 0)
            {
                progressService?
                    .SetProgressPercentage(100)
                    .SetProgressMessage(
                        "No database changes are required for this CSV.",
                        logToConsole: false);
            }
            else
            {
                progressService?
                    .SetTotalAmount(batches.Count)
                    .SetProgressMessage(
                        $"Saving {molensToImport.Count:N0} mills in " +
                        $"{batches.Count:N0} database batches...",
                        logToConsole: false);
            }

            var savedBatchCount = 0;
            var lastReportedBatchPercentage = -1;

            foreach (var batch in batches)
            {
                token.ThrowIfCancellationRequested();

                var batchList = batch.ToList();

                var existingBatch = batchList
                    .Where(molen => molen.Id != Guid.Empty)
                    .ToList();

                var newBatch = batchList
                    .Where(molen => molen.Id == Guid.Empty)
                    .ToList();

                if (existingBatch.Count > 0)
                {
                    await _dbMolenDataService.UpdateRange(existingBatch, token);
                    await _dbContext.SaveChangesAsync(token);
                    _dbContext.ChangeTracker.Clear();
                }

                if (newBatch.Count > 0)
                {
                    await _dbMolenDataService.AddRangeAsync(
                        newBatch,
                        token
                    );

                    await _dbContext.SaveChangesAsync(token);
                    _dbContext.ChangeTracker.Clear();
                }

                savedBatchCount++;

                ReportProgress(
                    progressService,
                    savedBatchCount,
                    batches.Count,
                    ref lastReportedBatchPercentage);
            }

            progressService?.SetProgressMessage(
                $"CSV import finished: {result.ImportedRows:N0} imported, " +
                $"{result.AddedMolens:N0} added, {result.UpdatedMolens:N0} updated, " +
                $"{result.SkippedExistingMolens:N0} skipped as existing, " +
                $"{result.SkippedInvalidRows:N0} invalid.",
                logToConsole: false);

            return result;
        }

        private static void ReportProgress(
            ProgressService? progressService,
            int currentAmount,
            int totalAmount,
            ref int lastReportedPercentage)
        {
            if (progressService == null || totalAmount <= 0)
                return;

            var percentage = (int)Math.Floor(
                currentAmount / (double)totalAmount * 100);

            if (percentage == lastReportedPercentage)
                return;

            progressService.SetProgressAmount(currentAmount);

            lastReportedPercentage = percentage;
        }

        private static List<Dictionary<string, string>> ReadRows(Stream csvStream, MillDatabaseImportResult result, CancellationToken token)
        {
            var rows = new List<Dictionary<string, string>>();

            using var parser = new TextFieldParser(csvStream, new UTF8Encoding(false, true), true, true)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters("|");

            if (parser.EndOfData) return rows;

            var rawHeaders = parser.ReadFields() ?? [];
            var headers = rawHeaders.Select(NormalizeHeader).ToArray();

            if (!headers.Contains("id") && !headers.Contains("bron_id") && !headers.Contains("milldatabase_id"))
                throw new InvalidDataException("De CSV bevat geen id- of bron_id-kolom.");

            while (!parser.EndOfData)
            {
                token.ThrowIfCancellationRequested();
                string[]? fields;

                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException exception)
                {
                    result.SkippedInvalidRows++;
                    AddWarning(result, $"CSV-regel {exception.LineNumber} kon niet worden gelezen.");
                    continue;
                }

                if (fields == null || fields.All(string.IsNullOrWhiteSpace)) continue;

                result.TotalRows++;
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < headers.Length; index++)
                {
                    if (string.IsNullOrWhiteSpace(headers[index])) continue;
                    row[headers[index]] = index < fields.Length ? fields[index] : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static MolenData CreateMolen(
            Dictionary<string, string> row,
            string sourceId,
            string sourceReference,
            string name,
            double latitude,
            double longitude,
            string? sourceSymbol)
        {
            var constructionYear = Get(row, "construction_year", "bouwjaar");
            ParseConstructionYear(constructionYear, out var bouwjaar, out var bouwjaarStart, out var bouwjaarEinde);

            var visiting = CleanText(Get(row, "visiting", "bezoekinformatie", "openingstijden"));
            var website = NormalizeWebsite(Get(row, "website", "website_link_1", "website_1"));
            var imageUrl = Get(row, "picture_url", "afbeelding_url", "foto_url");
            var imageName = CleanText(Get(row, "picture_name", "afbeelding_naam", "foto_naam")) ?? $"milldatabase-{sourceId}";
            var photographer = CleanText(Get(row, "photographer_name", "fotograaf"));
            var pictureDate = CleanText(Get(row, "picture_date", "afbeeldingsdatum"));
            var photographerEmail = CleanText(Get(row, "photographer_email", "fotograaf_e_mail"));
            var photographerWebsite = CleanText(Get(row, "photographer_web", "fotograaf_website"));

            var molen = new MolenData
            {
                Name = name,
                Ten_Brugge_Nr = sourceReference,
                MolenTBN = new MolenTBN { Ten_Brugge_Nr = sourceReference },
                Bouwjaar = bouwjaar,
                BouwjaarStart = bouwjaarStart,
                BouwjaarEinde = bouwjaarEinde,
                Functie = TranslateDelimited(Get(row, "function_type", "functie"), FunctionTranslations),
                Doel = TranslateDelimited(Get(row, "use_today", "huidig_gebruik", "doel"), UseTranslations),
                Toestand = GetToestand(row, sourceSymbol),
                Bedrijfsvaardigheid = GetBedrijfsvaardigheid(GetCondition(row)),
                Plaats = FirstNotEmpty(Get(row, "city", "plaats"), Get(row, "municipality", "gemeente")),
                Adres = BuildAddress(Get(row, "street", "straat"), Get(row, "zip", "postcode"), FirstNotEmpty(Get(row, "city", "plaats"), Get(row, "municipality", "gemeente"))),
                Land = TranslateValue(Get(row, "country", "land"), CountryTranslations),
                Provincie = CleanText(Get(row, "state", "provincie")),
                Gemeente = FirstNotEmpty(Get(row, "municipality", "gemeente"), Get(row, "county", "graafschap"), Get(row, "district")),
                Streek = CleanText(Get(row, "county", "graafschap", "streek")),
                Plaatsaanduiding = CleanText(Get(row, "city_part", "plaatsdeel", "plaatsaanduiding")),
                PlaatsenVoorheen = CleanText(Get(row, "cityname_old", "oude_plaatsnaam")),
                Wiekvorm = TranslateDelimited(Get(row, "sails", "wieken_type", "wiekvorm"), SailTranslations),
                Kruiwerk = TranslateDelimited(Get(row, "kruehwerk", "kruiwerk"), WindingTranslations),
                Vlucht = CleanText(Get(row, "sails_avg", "vlucht")),
                Wieken = CreateSailsDescription(Get(row, "sails_count", "aantal_wieken"), Get(row, "sails", "wieken_type")),
                Krachtbron = TranslateDelimited(Get(row, "drive_power_recent", "huidige_aandrijfkracht", "krachtbron"), DrivePowerTranslations),
                Website = website,
                Eigenaar = CleanText(Get(row, "owner", "eigenaar")),
                Molenaar = CleanText(Get(row, "operator", "beheerder", "molenaar")),
                Openingstijden = visiting,
                OpenVoorPubliek = IsPubliclyVisitAble(visiting),
                OpenOpZaterdag = ContainsAny(visiting, "saturday", "zaterdag", "samstag"),
                OpenOpZondag = ContainsAny(visiting, "sunday", "zondag", "sonntag"),
                OpenOpAfspraak = ContainsAny(visiting, "appointment", "afspraak", "voranmeldung", "vereinbarung", "aanvraag"),
                Geschiedenis = CleanText(Get(row, "history", "geschiedenis")),
                Museuminformatie = CleanText(Get(row, "activites", "activiteiten")),
                UniekeEigenschap = CleanText(Get(row, "top_photomotive", "hoofdfotomotief")),
                Bijzonderheden = BuildDetails(row),
                Rad = TranslateDelimited(Get(row, "waterwheel_type", "waterrad_type"), WaterWheelTypeTranslations),
                RadDiameter = CleanText(Get(row, "waterwheel_diameter", "waterrad_diameter")),
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow,
                CanAddImages = true,
                AddedImages = [],
                DisappearedYearInfos = [],
                MolenMakers = CreateMakers(row, constructionYear),
                MolenTypeAssociations = CreateTypeAssociations(row),
                Images = CreateImages(imageUrl, imageName, photographer, pictureDate, photographerEmail, photographerWebsite)
            };

            return molen;
        }

        private static List<MolenTypeAssociation> CreateTypeAssociations(Dictionary<string, string> row)
        {
            var millType = Get(row, "mill_type_en", "mill_type", "molentype").ToLowerInvariant();
            var construction = Get(row, "special_construction", "bijzondere_constructie").ToLowerInvariant();
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawType in millType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                switch (rawType)
                {
                    case "post mill":
                        types.Add("Standerdmolen");
                        break;
                    case "paltrok mill":
                        types.Add("Paltrokmolen");
                        break;
                    case "hollow post mill":
                        types.Add("Wipmolen");
                        break;
                    case "tower mill":
                        types.Add("Torenmolen");
                        AddHeightType(types, construction);
                        break;
                    case "smock mill":
                    case "kantige molen":
                    case "holländermühle":
                    case "hollandermuhle":
                        types.Add("Kantige molen");
                        AddHeightType(types, construction);
                        if (!types.Contains("Beltmolen") && !types.Contains("Stellingmolen")) types.Add("Grondzeiler");
                        break;
                    case "combined wind and watermill":
                        types.Add("Windmolen");
                        types.Add("Watermolen");
                        break;
                    case "composite mill":
                    case "inverted windmill":
                        types.Add("Windmolen");
                        break;
                    case "motor mill":
                        types.Add("Motormolen");
                        break;
                    default:
                        if (!string.IsNullOrWhiteSpace(rawType)) types.Add(ToDutchDisplayValue(rawType));
                        break;
                }
            }

            if (types.Count == 0) types.Add("Windmolen");

            return types.Select(type => new MolenTypeAssociation
            {
                MolenType = new MolenType { Name = type }
            }).ToList();
        }

        private static void AddHeightType(HashSet<string> types, string construction)
        {
            if (construction.Contains("mound", StringComparison.OrdinalIgnoreCase)) types.Add("Beltmolen");
            if (construction.Contains("reefing stage", StringComparison.OrdinalIgnoreCase)) types.Add("Stellingmolen");
        }

        private static List<MolenImage> CreateImages(string? imageUrl, string imageName, string? photographer, string? pictureDate, string? photographerEmail, string? photographerWebsite)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) return [];

            var descriptionParts = new List<string> { "Bron: Mill Database" };
            if (!string.IsNullOrWhiteSpace(photographer)) descriptionParts.Add($"Fotograaf: {photographer}");
            if (!string.IsNullOrWhiteSpace(pictureDate)) descriptionParts.Add($"Datum: {pictureDate}");
            if (!string.IsNullOrWhiteSpace(photographerEmail)) descriptionParts.Add($"E-mail fotograaf: {photographerEmail}");
            if (!string.IsNullOrWhiteSpace(photographerWebsite)) descriptionParts.Add($"Website fotograaf: {photographerWebsite}");

            return
            [
                new MolenImage
                {
                    FilePath = imageUrl,
                    ExternalUrl = imageUrl,
                    Name = imageName,
                    Description = string.Join(". ", descriptionParts),
                    CanBeDeleted = false
                }
            ];
        }

        private static List<MolenMaker> CreateMakers(Dictionary<string, string> row, string constructionYear)
        {
            var maker = CleanText(Get(row, "constructer_mill", "molenmaker"));
            if (string.IsNullOrWhiteSpace(maker)) return [];

            return
            [
                new MolenMaker
                {
                    Name = maker,
                    Year = string.IsNullOrWhiteSpace(constructionYear) ? "Onbekend" : constructionYear.Trim()
                }
            ];
        }

        private static string? BuildDetails(Dictionary<string, string> row)
        {
            var details = new List<string>();
            AddDetail(details, "Mill Database ID", Get(row, "id", "bron_id", "milldatabase_id"));
            AddDetail(details, null, Get(row, "description", "beschrijving"));
            AddDetail(details, "Bronstatus", TranslateDelimited(GetCondition(row), ConditionTranslations));
            AddDetail(details, "Kaartsymbool", GetSymbol(row));
            AddDetail(details, "Bijzondere constructie", TranslateDelimited(Get(row, "special_construction", "bijzondere_constructie"), ConstructionTranslations));
            AddDetail(details, "Techniek", Get(row, "technique", "techniek"));
            AddDetail(details, "Historische functie", TranslateDelimited(Get(row, "function_historic", "historische_functie"), FunctionTranslations));
            AddDetail(details, "Huidig aandrijftype", TranslateDelimited(Get(row, "drive_type_recent", "huidig_aandrijftype"), DriveTypeTranslations));
            AddDetail(details, "Oorspronkelijke aandrijfkracht", TranslateDelimited(Get(row, "drive_power_originally", "oorspronkelijke_aandrijfkracht"), DrivePowerTranslations));
            AddDetail(details, "Oorspronkelijk aandrijftype", TranslateDelimited(Get(row, "drive_type_originally", "oorspronkelijk_aandrijftype"), DriveTypeTranslations));
            AddDetail(details, "Externe referentie", Get(row, "external_number", "extern_nummer"));
            AddDetail(details, "Interne referentie", Get(row, "internal_number", "intern_nummer"));
            AddDetail(details, "Contact", Get(row, "contact"));
            AddDetail(details, "E-mail", Get(row, "contact_mail", "contact_e_mail"));
            AddDetail(details, "Molenvereniging", Get(row, "mill_society", "molenvereniging"));
            AddDetail(details, "Watertoevoer", TranslateDelimited(Get(row, "water_feed", "watertoevoer"), WaterFeedTranslations));
            AddDetail(details, "Aantal waterraderen", Get(row, "waterwheel_count", "aantal_waterraderen"));
            AddDetail(details, "Turbine", Get(row, "turbine"));
            AddDetail(details, "Aantal turbines", Get(row, "turbine_count", "aantal_turbines"));
            AddDetail(details, "Activiteiten", Get(row, "activites", "activiteiten"));
            AddDetail(details, "Deelname molendag 1", Get(row, "participant_mill_day_1", "deelnemer_molendag_1"));
            AddDetail(details, "Activiteit molendag 1", Get(row, "activity_mill_day_1", "activiteit_molendag_1"));
            AddDetail(details, "Deelname molendag 2", Get(row, "participant_mill_day_2", "deelnemer_molendag_2"));
            AddDetail(details, "Activiteit molendag 2", Get(row, "activity_mill_day_2", "activiteit_molendag_2"));
            AddDetail(details, "Deelname molendag 3", Get(row, "participant_mill_day_3", "deelnemer_molendag_3"));
            AddDetail(details, "Activiteit molendag 3", Get(row, "activity_mill_day_3", "activiteit_molendag_3"));
            AddDetail(details, "Lid", Get(row, "member", "lid"));
            AddDetail(details, "Afbeelding-ID", Get(row, "picture", "afbeelding_id"));
            AddDetail(details, "Molenroute", Get(row, "mill_trail", "molenroute_naam"));
            AddDetail(details, "Waterloop", Get(row, "course_of_a_river", "waterloop"));
            AddDetail(details, "Buiten gebruik sinds", Get(row, "shutdown", "buiten_gebruik_sinds"));
            AddDetail(details, "Breedte waterrad", Get(row, "waterwheel_width", "waterrad_breedte"));
            AddDetail(details, "Remtype", Get(row, "breake_type", "remtype"));
            AddDetail(details, "Gebouwhoogte", Get(row, "building_height", "gebouwhoogte"));
            AddDetail(details, "Aantal verdiepingen", Get(row, "floors_count", "aantal_verdiepingen"));
            AddDetail(details, "Overige gebouwen", Get(row, "other_buildings", "overige_gebouwen"));
            AddDetail(details, "Website 2", Get(row, "website_link_2"));
            AddDetail(details, "Website 3", Get(row, "website_link_3"));
            AddDetail(details, "Website 4", Get(row, "website_link_4"));
            AddDetail(details, "Website 5", Get(row, "website_link_5"));
            AddDetail(details, "Website 6", Get(row, "website_link_6"));
            AddDetail(details, "Molenroute", Get(row, "mill_road", "molenroute"));
            return details.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, details);
        }

        private static void AddDetail(List<string> details, string? heading, string? value)
        {
            var cleaned = CleanText(value);
            if (string.IsNullOrWhiteSpace(cleaned)) return;
            details.Add(string.IsNullOrWhiteSpace(heading) ? cleaned : $"{heading}: {cleaned}");
        }

        private static bool TryParseCoordinate(string? value, out double coordinate, out bool corrected)
        {
            corrected = false;
            coordinate = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var normalized = value.Trim().Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out coordinate)) return true;

            var correctedValue = Regex.Replace(normalized, @"(?<=\d)[oO](?=$)", "0");
            if (correctedValue == normalized || !double.TryParse(correctedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out coordinate)) return false;

            corrected = true;
            return true;
        }

        private static void ParseConstructionYear(string? value, out int? year, out int? startYear, out int? endYear)
        {
            year = null;
            startYear = null;
            endYear = null;
            if (string.IsNullOrWhiteSpace(value)) return;

            var years = Regex.Matches(value, @"(?<!\d)(1[0-9]{3}|20[0-9]{2})(?!\d)")
                .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture))
                .Distinct()
                .Order()
                .ToList();

            if (years.Count == 1) year = years[0];
            else if (years.Count > 1)
            {
                startYear = years.First();
                endYear = years.Last();
            }
        }

        private static string? BuildAddress(string? street, string? zip, string? city)
        {
            street = CleanText(street);
            zip = CleanText(zip);
            city = CleanText(city);
            var secondLine = string.Join(" ", new[] { zip, city }.Where(value => !string.IsNullOrWhiteSpace(value)));
            var lines = new[] { street, secondLine }.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            return lines.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, lines);
        }

        private static string? CreateSailsDescription(string? sailsCount, string? sails)
        {
            var values = new List<string>();
            var count = CleanText(sailsCount);
            var translatedSails = TranslateDelimited(sails, SailTranslations);
            if (!string.IsNullOrWhiteSpace(count)) values.Add($"Aantal wieken: {count}");
            if (!string.IsNullOrWhiteSpace(translatedSails)) values.Add(translatedSails);
            return values.Count == 0 ? null : string.Join("; ", values);
        }

        private static string? NormalizeWebsite(string? value)
        {
            value = CleanText(value);
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (Uri.IsWellFormedUriString(value, UriKind.Absolute)) return value;
            return Uri.IsWellFormedUriString("https://" + value, UriKind.Absolute) ? "https://" + value : value;
        }

        private static string GetToestand(
            Dictionary<string, string> row,
            string? sourceSymbol)
        {
            var conditions = SplitValues(GetCondition(row));
            var symbols = SplitValues(GetSymbol(row));

            if (!string.IsNullOrWhiteSpace(sourceSymbol))
                symbols.Add(sourceSymbol.Trim().ToLowerInvariant());

            if (conditions.Any(condition =>
                    condition is "functional" or "functioneel" or "werkend")
                || symbols.Contains("windmill_work"))
            {
                return MolenToestand.Werkend;
            }

            if (conditions.Any(condition =>
                    condition is "remains" or "remainder" or "ruin" or
                        "ruins" or "restant")
                || symbols.Any(symbol =>
                    symbol is "windmill_ruin" or "ruin" or "ruins" or
                        "restant"))
            {
                return MolenToestand.Restant;
            }

            if (conditions.Any(condition =>
                    condition is "gone" or "disappeared" or "verdwenen" or
                        "no longer exists")
                || symbols.Any(symbol =>
                    symbol is "windmill_gone" or "gone" or "disappeared" or
                        "verdwenen"))
            {
                return MolenToestand.Verdwenen;
            }

            if (conditions.Any(condition =>
                    condition is "under construction" or "in aanbouw" or
                        "inaanbouw" or "in restoration" or "in restauratie"))
            {
                return MolenToestand.InAanbouw;
            }

            if (conditions.Any(condition =>
                    condition is "stored" or "opgeslagen" or "in opslag"))
            {
                return MolenToestand.Opgeslagen;
            }

            if (conditions.Any(condition =>
                    condition is "dismantled" or "gedemonteerd"))
            {
                return MolenToestand.Gedemonteerd;
            }

            if (conditions.Any(condition =>
                    condition is "not functional" or "niet functioneel" or
                        "niet werkend" or "no technique" or
                        "geen techniek" or "geen techniek aanwezig"))
            {
                return MolenToestand.NietWerkend;
            }

            return MolenToestand.Bestaande;
        }

        private static string GetCondition(Dictionary<string, string> row)
        {
            return Get(
                row,
                "condition_type_en",
                "condition_type_english",
                "current_condition_en",
                "condition_type",
                "condition",
                "current_condition",
                "current_condition_type",
                "toestand");
        }

        private static string GetSymbol(Dictionary<string, string> row)
        {
            return Get(
                row,
                "logo_detail_en",
                "logo_detail",
                "symbol",
                "symbols",
                "map_symbol",
                "map_icon",
                "icon");
        }

        private static string GetBedrijfsvaardigheid(string? condition)
        {
            var translated = TranslateDelimited(condition, ConditionTranslations);
            return string.IsNullOrWhiteSpace(translated) ? "Onbekend" : translated;
        }

        private static HashSet<string> SplitValues(string? value)
        {
            return Regex
                .Split(value ?? string.Empty, @"[;,|]+")
                .Select(part => part.Trim().ToLowerInvariant())
                .Where(part => part.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPubliclyVisitAble(string? visiting)
        {
            if (string.IsNullOrWhiteSpace(visiting)) return false;
            return !ContainsAny(visiting, "no", "nee", "nicht möglich", "not possible");
        }

        private static bool ContainsAny(string? value, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return values.Any(search => value.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private static string? TranslateDelimited(string? value, IReadOnlyDictionary<string, string> translations)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var translated = Regex.Split(value, @"[;,|]+")
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .Select(part => TranslateValue(part, translations))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return translated.Count == 0 ? null : string.Join(", ", translated);
        }

        private static string? TranslateValue(string? value, IReadOnlyDictionary<string, string> translations)
        {
            value = CleanText(value);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return translations.TryGetValue(value, out var translated) ? translated : ToDutchDisplayValue(value);
        }

        private static string ToDutchDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return char.ToUpperInvariant(value[0]) + value[1..];
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var normalizedHtml = Regex.Replace(value, @"<\s*br\s*/?\s*>", Environment.NewLine, RegexOptions.IgnoreCase);
            normalizedHtml = Regex.Replace(normalizedHtml, @"</\s*p\s*>", Environment.NewLine, RegexOptions.IgnoreCase);
            var document = new HtmlDocument();
            document.LoadHtml(normalizedHtml);
            var text = WebUtility.HtmlDecode(document.DocumentNode.InnerText).Replace('\u00A0', ' ');
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\s*\r?\n\s*", Environment.NewLine);
            text = Regex.Replace(text, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string Get(Dictionary<string, string> row, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (row.TryGetValue(NormalizeHeader(alias), out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return string.Empty;
        }

        private static string GetExternalReference(Dictionary<string, string> row)
        {
            var externalNumber = Get(
                row,
                "external_number",
                "extern_nummer",
                "ten_brugge_nummer",
                "ten_brugge_nr",
                "tbn"
            );

            if (!string.IsNullOrWhiteSpace(externalNumber))
                return externalNumber;

            for (var websiteNumber = 1; websiteNumber <= 6; websiteNumber++)
            {
                var website = Get(row, $"website_link_{websiteNumber}");
                if (string.IsNullOrWhiteSpace(website)) continue;

                var match = Regex.Match(
                    website,
                    @"ten[-_/]?bruggencate(?:nummer|-nr)?[-_/]([0-9a-z-]+)",
                    RegexOptions.IgnoreCase
                );

                if (match.Success) return match.Groups[1].Value;
            }

            return string.Empty;
        }

        private static IReadOnlyList<string> GetNormalizedExternalReferences(
            Dictionary<string, string> row)
        {
            return Regex
                .Split(GetExternalReference(row), @"[;,|]+")
                .Select(NormalizeExternalReference)
                .Where(reference => !string.IsNullOrWhiteSpace(reference))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetExternalReferenceAliases(
            ExistingMolenReference molen)
        {
            if (!IsMillDatabaseReference(molen.TenBruggeNr))
            {
                var directReference = NormalizeExternalReference(molen.TenBruggeNr);

                if (!string.IsNullOrWhiteSpace(directReference))
                    yield return directReference;
            }

            if (string.IsNullOrWhiteSpace(molen.Details))
                yield break;

            var matches = Regex.Matches(
                molen.Details,
                @"(?:^|\r?\n)\s*Externe referentie\s*:\s*(?<value>[^\r\n]+)",
                RegexOptions.IgnoreCase
            );

            foreach (Match match in matches)
            {
                foreach (var reference in Regex.Split(
                    match.Groups["value"].Value,
                    @"[;,|]+"))
                {
                    var normalizedReference = NormalizeExternalReference(reference);

                    if (!string.IsNullOrWhiteSpace(normalizedReference))
                        yield return normalizedReference;
                }
            }
        }

        private static bool IsMillDatabaseReference(string? reference)
        {
            return reference?.Trim().StartsWith(
                SourceReferencePrefix,
                StringComparison.OrdinalIgnoreCase
            ) == true;
        }

        private static string NormalizeExternalReference(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = NormalizeReference(value);

            var knownPrefixes = new[]
            {
                "tenbruggencatenummer",
                "tenbruggencatenr",
                "tenbruggencate",
                "tenbruggenummer",
                "tenbruggenr",
                "tenbrugge",
                "tbn"
            };

            foreach (var prefix in knownPrefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && normalized.Length > prefix.Length)
                {
                    return normalized[prefix.Length..];
                }
            }

            return normalized;
        }

        private static string? FirstNotEmpty(params string?[] values)
        {
            return values.Select(CleanText).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private static string NormalizeReference(string value)
        {
            return Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", string.Empty);
        }

        private static string NormalizeHeader(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace('é', 'e').Replace('ë', 'e').Replace('ï', 'i');
            return Regex.Replace(normalized, @"[^a-z0-9]+", "_").Trim('_');
        }

        private static void AddWarning(MillDatabaseImportResult result, string warning)
        {
            if (result.Warnings.Count < 100) result.Warnings.Add(warning);
        }

        private sealed class ExistingMolenReference
        {
            public Guid Id { get; init; }
            public Guid MolenTBNId { get; init; }
            public required string TenBruggeNr { get; init; }
            public string? Details { get; init; }
        }

        private static readonly Dictionary<string, string> ConditionTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["functional"] = "Bedrijfsvaardig",
            ["functioneel"] = "Bedrijfsvaardig",
            ["werkend"] = "Bedrijfsvaardig",
            ["not functional"] = "Niet bedrijfsvaardig",
            ["niet functioneel"] = "Niet bedrijfsvaardig",
            ["in restoration"] = "In restauratie",
            ["in restauratie"] = "In restauratie",
            ["dismantled"] = "Gedemonteerd",
            ["gedemonteerd"] = "Gedemonteerd",
            ["no technique"] = "Geen techniek aanwezig",
            ["geen techniek"] = "Geen techniek aanwezig",
            ["remains"] = "Restant",
            ["restant"] = "Restant",
            ["stored"] = "Opgeslagen",
            ["opgeslagen"] = "Opgeslagen",
            ["technique"] = "Techniek aanwezig",
            ["techniek"] = "Techniek aanwezig",
            ["under construction"] = "In aanbouw",
            ["in aanbouw"] = "In aanbouw",
            ["gone"] = "Verdwenen",
            ["disappeared"] = "Verdwenen",
            ["no longer exists"] = "Verdwenen",
            ["ruin"] = "Restant",
            ["remainder"] = "Restant"
        };

        private static readonly Dictionary<string, string> ConstructionTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["four-sided"] = "Vierkant",
            ["four-sided substructure"] = "Vierkante onderbouw",
            ["hexagonal"] = "Zeskantig",
            ["hexagonal trestle with three crosstrees"] = "Zeskantige bok met drie kruisplaten",
            ["mound"] = "Molenbelt",
            ["octagonal"] = "Achtkantig",
            ["octagonal roundhouse"] = "Achtkante onderbouw",
            ["open trestle"] = "Open voet",
            ["partly enclosed trestle"] = "Gedeeltelijk gesloten voet",
            ["pedestal"] = "Voetstuk",
            ["petit pied"] = "Petit pied",
            ["reefing stage"] = "Stelling",
            ["roundhouse"] = "Ronde onderbouw",
            ["sixteen sided"] = "Zestienkantig",
            ["sunken foot"] = "Verzonken voet",
            ["ten sided"] = "Tienkantig",
            ["triangular"] = "Driehoekig",
            ["twelve sided"] = "Twaalfkantig",
            ["twelve-sided"] = "Twaalfkantig"
        };

        private static readonly Dictionary<string, string> DriveTypeTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sails"] = "Wieken",
            ["electric motor"] = "Elektromotor",
            ["diesel"] = "Dieselmotor",
            ["steam"] = "Stoommachine",
            ["waterwheel"] = "Waterrad"
        };

        private static readonly Dictionary<string, string> CountryTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Antigua & Barbuda"] = "Antigua en Barbuda",
            ["Argentina"] = "Argentinië",
            ["Australia"] = "Australië",
            ["Austria"] = "Oostenrijk",
            ["Barbados"] = "Barbados",
            ["Belarus"] = "Belarus",
            ["Belgium"] = "België",
            ["Brazil"] = "Brazilië",
            ["Bulgaria"] = "Bulgarije",
            ["Canada"] = "Canada",
            ["Croatia"] = "Kroatië",
            ["Curaçao"] = "Curaçao",
            ["Cyprus"] = "Cyprus",
            ["Czech Republic"] = "Tsjechië",
            ["Denmark"] = "Denemarken",
            ["Dominica"] = "Dominica",
            ["Egypt"] = "Egypte",
            ["Estonia"] = "Estland",
            ["Finland"] = "Finland",
            ["France"] = "Frankrijk",
            ["Germany"] = "Duitsland",
            ["Greece"] = "Griekenland",
            ["Hungary"] = "Hongarije",
            ["Ireland"] = "Ierland",
            ["Israel"] = "Israël",
            ["Italy"] = "Italië",
            ["Jamaica"] = "Jamaica",
            ["Japan"] = "Japan",
            ["Latvia"] = "Letland",
            ["Lithuania"] = "Litouwen",
            ["Malta"] = "Malta",
            ["Mauritius"] = "Mauritius",
            ["Moldova"] = "Moldavië",
            ["Montserrat"] = "Montserrat",
            ["Netherlands"] = "Nederland",
            ["New Zealand"] = "Nieuw-Zeeland",
            ["Poland"] = "Polen",
            ["Portugal"] = "Portugal",
            ["Romania"] = "Roemenië",
            ["Russia"] = "Rusland",
            ["Serbia"] = "Servië",
            ["Slovakia"] = "Slowakije",
            ["Slovenia"] = "Slovenië",
            ["South Africa"] = "Zuid-Afrika",
            ["Spain"] = "Spanje",
            ["St Kitts and Nevis"] = "Saint Kitts en Nevis",
            ["St Vincent and the Grenadines"] = "Saint Vincent en de Grenadines",
            ["Sweden"] = "Zweden",
            ["Taiwan"] = "Taiwan",
            ["Trinidad and Tobago"] = "Trinidad en Tobago",
            ["Turkey"] = "Turkije",
            ["Ukraine"] = "Oekraïne",
            ["United Kingdom"] = "Verenigd Koninkrijk",
            ["United States of America"] = "Verenigde Staten",
            ["Uruguay"] = "Uruguay"
        };

        private static readonly Dictionary<string, string> FunctionTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["agricultural use"] = "Landbouwgebruik",
            ["bone crusher"] = "Beenderenbreker",
            ["brine pump"] = "Pekelpomp",
            ["cement mill"] = "Cementmolen",
            ["chalk mill"] = "Krijtmolen",
            ["chicory mill"] = "Cichoreimolen",
            ["churn"] = "Karnmolen",
            ["drainage"] = "Poldermolen",
            ["dyewood mill"] = "Verfhoutmolen",
            ["electricity"] = "Elektriciteitsopwekking",
            ["flax mill"] = "Vlasmolen",
            ["grain mill"] = "Korenmolen",
            ["grindstone"] = "Slijpsteen",
            ["grist mill"] = "Maalmolen",
            ["gritstone mill"] = "Slijpsteenmolen",
            ["hemp beater"] = "Hennepklopper",
            ["hulling mill"] = "Pelmolen",
            ["lathe"] = "Draaibank",
            ["millet stamp"] = "Gierststamper",
            ["mustard mill"] = "Mosterdmolen",
            ["oil mill"] = "Oliemolen",
            ["ore crusher"] = "Ertsbreker",
            ["ornament"] = "Siermolen",
            ["paper mill"] = "Papiermolen",
            ["polishing mill"] = "Polijstmolen",
            ["pump mill"] = "Pompmolen",
            ["pumping station"] = "Gemaal",
            ["salt crushing"] = "Zoutbreker",
            ["saw mill"] = "Zaagmolen",
            ["scouring powder mill"] = "Schuurpoedermolen",
            ["shingle machine"] = "Dakspaanmachine",
            ["snuff mill"] = "Snuifmolen",
            ["spice mill"] = "Specerijenmolen",
            ["sugar mill"] = "Suikermolen",
            ["tanbark mill"] = "Runmolen",
            ["windpump"] = "Windpomp",
            ["woodwork shop"] = "Houtwerkplaats"
        };

        private static readonly Dictionary<string, string> UseTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["activity centre"] = "Activiteitencentrum",
            ["animal shelter"] = "Dierenverblijf",
            ["apartment building"] = "Appartementengebouw",
            ["art gallery"] = "Kunstgalerie",
            ["cafe"] = "Café",
            ["café"] = "Café",
            ["chapel"] = "Kapel",
            ["clubhouse"] = "Clubgebouw",
            ["community centre"] = "Buurthuis",
            ["cultural centre"] = "Cultureel centrum",
            ["demonstration site"] = "Demonstratielocatie",
            ["dental practice"] = "Tandartspraktijk",
            ["dovecote"] = "Duiventil",
            ["education centre"] = "Educatiecentrum",
            ["event centre"] = "Evenementencentrum",
            ["exhibition space"] = "Expositieruimte",
            ["extracurricular learning site"] = "Buitenschoolse leerlocatie",
            ["farm supply"] = "Agrarische handel",
            ["flour factory"] = "Meelfabriek",
            ["function hall"] = "Zaal",
            ["function room"] = "Ontvangstruimte",
            ["garage"] = "Garage",
            ["gift shop"] = "Cadeauwinkel",
            ["grain storage"] = "Graanopslag",
            ["greenhouse"] = "Kas",
            ["heritage centre"] = "Erfgoedcentrum",
            ["holiday accommodation"] = "Vakantieverblijf",
            ["hotel"] = "Hotel",
            ["industrial mill"] = "Industriële molen",
            ["information centre"] = "Informatiecentrum",
            ["library"] = "Bibliotheek",
            ["lodging"] = "Logies",
            ["memorial"] = "Gedenkteken",
            ["mill shop"] = "Molenwinkel",
            ["museum"] = "Museum",
            ["nursery school"] = "Peuterspeelzaal",
            ["observation tower"] = "Uitkijktoren",
            ["observatory"] = "Sterrenwacht",
            ["office"] = "Kantoor",
            ["party centre"] = "Feestlocatie",
            ["pedestal"] = "Voetstuk",
            ["private residence"] = "Woning",
            ["pub"] = "Café",
            ["registry office"] = "Trouwlocatie",
            ["restaurant"] = "Restaurant",
            ["sauna cottage"] = "Saunahuisje",
            ["senior residence"] = "Seniorenwoning",
            ["shop"] = "Winkel",
            ["staff and volunteer accommodation"] = "Personeels- en vrijwilligersverblijf",
            ["storage space"] = "Opslagruimte",
            ["studio"] = "Atelier",
            ["tearoom"] = "Theeschenkerij",
            ["telecom terminal"] = "Telecominstallatie",
            ["tourist office"] = "VVV-kantoor",
            ["town hall"] = "Gemeentehuis",
            ["training centre"] = "Opleidingscentrum",
            ["visitor centre"] = "Bezoekerscentrum",
            ["water cistern"] = "Waterreservoir",
            ["weather station"] = "Weerstation",
            ["workshop"] = "Werkplaats",
            ["workshop for the disabled"] = "Sociale werkplaats",
            ["youth centre"] = "Jeugdcentrum"
        };

        private static readonly Dictionary<string, string> DrivePowerTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["windmill"] = "Windkracht",
            ["engine driven"] = "Motoraandrijving",
            ["watermill"] = "Waterkracht",
            ["none"] = "Geen"
        };

        private static readonly Dictionary<string, string> WaterFeedTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["breastshot"] = "Middenslag",
            ["undershot"] = "Onderslag"
        };

        private static readonly Dictionary<string, string> WaterWheelTypeTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["breastshot"] = "Middenslagrad",
            ["overshot"] = "Bovenslagrad",
            ["undershot"] = "Onderslagrad"
        };

        private static readonly Dictionary<string, string> SailTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cloth"] = "Zeilvoering",
            ["shuttered"] = "Zelfzwichting",
            ["board"] = "Bordwieken",
            ["jib"] = "Jibzeilen",
            ["aerodynamic"] = "Aerodynamisch",
            ["dummy"] = "Dummywieken",
            ["stocks"] = "Roeden",
            ["annular"] = "Ringvormig",
            ["horizontal"] = "Horizontaal",
            ["experimental"] = "Experimenteel"
        };

        private static readonly Dictionary<string, string> WindingTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["fantail"] = "Zelfkruiing met windroos",
            ["double fantail"] = "Zelfkruiing met dubbele windroos",
            ["tailpole"] = "Staartkruiing",
            ["wheel-and-chain winding"] = "Kruirad met ketting",
            ["wind vane"] = "Windvaan",
            ["internal winding system"] = "Intern kruiwerk"
        };
    }
}