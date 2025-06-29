using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("place")]
    public class Place : DefaultModel, IEquatable<Place>
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Province { get; set; }
        public required string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Population { get; set; }
        [ForeignKey("PlaceTypeId")]
        public int? PlaceTypeId { get; set; }
        public PlaceType? Type { get; set; }

        public bool Equals(Place? other)
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Province.Equals(other.Province, StringComparison.OrdinalIgnoreCase) &&
                   Latitude == other.Latitude &&
                   Longitude == other.Longitude;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Place other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.ToLowerInvariant(), Province.ToLowerInvariant(), Latitude, Longitude);
        }
    }
}
