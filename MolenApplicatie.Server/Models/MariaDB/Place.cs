using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("place")]
    public class Place
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Province { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Population { get; set; }
    }
}
