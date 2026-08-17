namespace MolenApplicatie.Server.Models
{
    public class MolenFilters
    {
        public List<ValueName> Landen { get; set; } = new List<ValueName>();
        public List<ValueName> Provincies { get; set; } = new List<ValueName>();
        public List<ValueName> Toestanden { get; set; } = new List<ValueName>();
        public List<ValueName> Types { get; set; } = new List<ValueName>();
    }

    public class ValueName
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? Parent { get; set; }
    }
}
