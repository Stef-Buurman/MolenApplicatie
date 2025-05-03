using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_maker")]
    public class MolenMaker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Year { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }
    }
}
