using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_image")]
    public class MolenImage : DefaultModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string FilePath { get; set; }
        public required string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public string? Description { get; set; }
        public required string ExternalUrl { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }
        public bool IsAddedImage = false;
    }
}