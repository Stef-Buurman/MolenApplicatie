using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type_association")]
    public class MolenTypeAssociation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MolenTypeAssociationId { get; set; }
        public int MolenDataId { get; set; }
        public int MolenTypeId { get; set; }
        [JsonIgnore]
        public virtual MolenData MolenData { get; set; } = null!;
        [JsonIgnore]
        public virtual MolenType MolenType { get; set; } = null!;
    }
}
