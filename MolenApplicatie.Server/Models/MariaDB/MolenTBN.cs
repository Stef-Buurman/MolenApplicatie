using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_tbn")]
    public class MolenTBN
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Ten_Brugge_Nr { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }
    }
}
