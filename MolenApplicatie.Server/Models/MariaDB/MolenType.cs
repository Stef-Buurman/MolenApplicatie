using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_type")]
    public class MolenType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<MolenTypeAssociation> MolenTypeAssociations { get; set; } = new List<MolenTypeAssociation>();
    }
}
