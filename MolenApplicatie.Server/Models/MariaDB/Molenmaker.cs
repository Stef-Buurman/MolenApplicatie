using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_maker")]
    public class MolenMaker : DefaultModel, IEquatable<MolenMaker>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Year { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }

        public bool Equals(MolenMaker? other)
        {
            if (other == null) return false;
            return Name == other.Name &&
                   Year == other.Year &&
                   MolenDataId == other.MolenDataId;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenMaker other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Year, MolenDataId);
        }
    }
}
