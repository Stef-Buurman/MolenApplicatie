namespace MolenApplicatie.Server.Models
{
    public class MolenMapFilter
    {
        public double West { get; set; }
        public double South { get; set; }
        public double East { get; set; }
        public double North { get; set; }
        public int Zoom { get; set; }

        public string? MolenType { get; set; }
        public string? Land { get; set; }
        public string? Provincie { get; set; }
        public string? MolenState { get; set; }
        public bool? HasImage { get; set; }
    }
}
