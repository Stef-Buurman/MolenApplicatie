namespace MolenApplicatie.Server.Models
{
    public class MillDatabaseImportResult
    {
        public int TotalRows { get; set; }
        public int ImportedRows { get; set; }
        public int AddedMolens { get; set; }
        public int UpdatedMolens { get; set; }
        public int SkippedExistingMolens { get; set; }
        public int SkippedInvalidRows { get; set; }
        public int SkippedUnsupportedMolenTypes { get; set; }
        public string? SearchUrl { get; set; }
        public string? CsvUrl { get; set; }
        public List<string> SearchUrls { get; set; } = [];
        public List<string> CsvUrls { get; set; } = [];
        public List<string> CompletedImports { get; set; } = [];
        public Dictionary<string, int> ToestandCounts { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; set; } = [];
    }
}
