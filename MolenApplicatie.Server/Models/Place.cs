using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class Place
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Population { get; set; }
    }
}
