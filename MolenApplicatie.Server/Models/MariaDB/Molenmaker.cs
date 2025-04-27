using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_maker")]
    public class MolenMaker
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Year { get; set; }
        public MolenData MolenData { get; set; }
        public int MolenDataId { get; set; }
    }
}
