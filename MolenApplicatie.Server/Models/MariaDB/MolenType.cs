using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type")]
    public class MolenType : DefaultModel, IEquatable<MolenType>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public required string Name { get; set; }
        [JsonIgnore]
        public virtual List<MolenTypeAssociation>? MolenTypeAssociations { get; set; } = null!;

        public bool Equals(MolenType? other)
        {
            if (other == null) return false;
            return Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenType other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}
