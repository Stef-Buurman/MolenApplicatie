using Microsoft.VisualBasic.FileIO;
using MolenApplicatie.Server.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace MolenApplicatie.Server.Services
{
    internal static class MillDatabaseCsvReader
    {
        public static List<Dictionary<string, string>> ReadRows(
            Stream csvStream,
            MillDatabaseImportResult result,
            CancellationToken token)
        {
            var rows = new List<Dictionary<string, string>>();

            using var parser = new TextFieldParser(
                csvStream,
                new UTF8Encoding(false, true),
                true,
                true)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };

            parser.SetDelimiters("|");

            if (parser.EndOfData) return rows;

            var rawHeaders = parser.ReadFields() ?? [];
            var headers = rawHeaders.Select(NormalizeHeader).ToArray();

            if (!headers.Contains("id") &&
                !headers.Contains("bron_id") &&
                !headers.Contains("milldatabase_id"))
            {
                throw new InvalidDataException(
                    "De CSV bevat geen id- of bron_id-kolom.");
            }

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
                    AddWarning(
                        result,
                        $"CSV-regel {exception.LineNumber} kon niet worden gelezen.");
                    continue;
                }

                if (fields == null || fields.All(string.IsNullOrWhiteSpace))
                    continue;

                result.TotalRows++;
                var row = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < headers.Length; index++)
                {
                    if (string.IsNullOrWhiteSpace(headers[index])) continue;

                    row[headers[index]] =
                        index < fields.Length ? fields[index] : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        internal static string NormalizeHeader(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            normalized = normalized
                .Replace('é', 'e')
                .Replace('ë', 'e')
                .Replace('ï', 'i');

            return Regex.Replace(normalized, @"[^a-z0-9]+", "_")
                .Trim('_');
        }

        private static void AddWarning(
            MillDatabaseImportResult result,
            string warning)
        {
            if (result.Warnings.Count < 100)
                result.Warnings.Add(warning);
        }
    }
}
