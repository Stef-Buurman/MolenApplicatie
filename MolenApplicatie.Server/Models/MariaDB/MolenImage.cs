using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_image")]
    public class MolenImage
    {
        [Key]
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public string Description { get; set; }
        public string ExternalUrl { get; set; }
        public MolenData MolenData { get; set; }
        public int MolenDataId { get; set; }
        public bool IsAddedImage { get; set; } = false;
    }
}