using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("added_image")]
    public class AddedImage : DefaultModel, IEquatable<AddedImage>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string FilePath { get; set; }
        public required string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public DateTime? DateTaken { get; set; }
        public string? Description { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public int MolenDataId { get; set; }
        public bool IsAddedImage = true;

        public bool Equals(AddedImage? other)
        {
            if (other == null) return false;
            return FilePath == other.FilePath;
        }
        public override bool Equals(object? obj)
        {
            if (obj is AddedImage other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FilePath);
        }
    }
}
