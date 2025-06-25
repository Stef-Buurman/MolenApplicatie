using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type_association")]
    public class MolenTypeAssociation : DefaultModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MolenDataId { get; set; }
        public int MolenTypeId { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public MolenType MolenType { get; set; } = null!;
    }
}
