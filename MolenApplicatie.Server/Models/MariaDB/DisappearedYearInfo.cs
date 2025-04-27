using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("disappeared_year_info")]
    public class DisappearedYearInfo
    {
        [Key]
        public int Id { get; set; }
        public string Status_before { get; set; }
        public int Year { get; set; }
        public string Status_after { get; set; }
        public MolenData MolenData { get; set; }
        public int MolenDataId { get; set; }
    }
}
