using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type_association")]
    public class MolenTypeAssociation : DefaultModel, IEquatable<MolenTypeAssociation>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MolenDataId { get; set; }
        public int MolenTypeId { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public MolenType MolenType { get; set; } = null!;

        public bool Equals(MolenTypeAssociation? other)
        {
            if (other == null) return false;
            return MolenDataId == other.MolenDataId &&
                   MolenTypeId == other.MolenTypeId;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenTypeAssociation other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MolenDataId, MolenTypeId);
        }
    }
}
