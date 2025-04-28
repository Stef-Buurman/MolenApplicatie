using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("added_image")]
    public class AddedImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? Description { get; set; }
        public MolenData MolenData { get; set; }
        public int MolenDataId { get; set; }
        public bool IsAddedImage = true;
    }
}
