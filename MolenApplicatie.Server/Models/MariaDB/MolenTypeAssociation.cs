using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type_association")]
    public class MolenTypeAssociation
    {
        [Key]
        public int MolenTypeAssociationId { get; set; }
        public int MolenDataId { get; set; }
        public int MolenTypeId { get; set; }
        public virtual MolenData MolenData { get; set; }
        public virtual MolenType MolenType { get; set; }
    }
}
