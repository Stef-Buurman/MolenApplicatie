namespace MolenApplicatie.Server.Models.MariaDB
{
    public class MolenLatLongReturn
    {
        public Guid MolenID { get; set; }
        public string MolenTBN { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool HasImage { get; set; }
    }
}
