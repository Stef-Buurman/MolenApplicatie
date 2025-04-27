using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("last_searched_for_new_data")]
    public class LastSearchedForNewData
    {
        [Key]
        public int Id { get; set; }
        public DateTime LastSearched { get; set; }
    }
}
