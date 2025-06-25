using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("place")]
    public class Place : DefaultModel
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Province { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Population { get; set; }
    }
}
