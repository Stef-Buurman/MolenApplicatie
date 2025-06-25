using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("last_searched_for_new_data")]
    public class LastSearchedForNewData : DefaultModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime LastSearched { get; set; }
    }
}
