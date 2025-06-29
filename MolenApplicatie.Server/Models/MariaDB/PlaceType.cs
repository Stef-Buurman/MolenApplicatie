using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("place_type")]
    public class PlaceType : DefaultModel, IEquatable<PlaceType>
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string NameEn { get; set; }
        public required string NameMV { get; set; }
        public required string Group { get; set; }
        public List<Place>? Places { get; set; }

        public bool Equals(PlaceType? other)
        {
            if (other == null) return false;
            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase)
                && Group.Equals(other.Group, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (obj is PlaceType other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.ToLowerInvariant(), Group.ToLowerInvariant());
        }
    }
}
