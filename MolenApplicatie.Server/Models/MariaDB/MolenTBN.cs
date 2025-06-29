using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_tbn")]
    public class MolenTBN : DefaultModel, IEquatable<MolenTBN>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Ten_Brugge_Nr { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }

        public bool Equals(MolenTBN? other)
        {
            if (other == null) return false;
            return Ten_Brugge_Nr == other.Ten_Brugge_Nr;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenTBN other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ten_Brugge_Nr);
        }
    }
}
