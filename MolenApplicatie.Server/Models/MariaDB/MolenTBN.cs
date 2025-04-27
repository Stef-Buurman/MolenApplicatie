using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_btn")]
    public class MolenTBN
    {
        [Key]
        public int Id { get; set; }
        public string Ten_Brugge_Nr { get; set; }
        public MolenData MolenData { get; set; }
        public int MolenDataId { get; set; }
    }
}
