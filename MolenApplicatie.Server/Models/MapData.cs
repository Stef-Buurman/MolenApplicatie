namespace MolenApplicatie.Server.Models
{
    public class MapData
    {
        public string Reference { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Toestand { get; set; } = null!;
        public bool HasImage { get; set; } = false;
        public string Type { get; set; } = null!;
        public List<string> Types { get; set; } = null!;
    }
}
