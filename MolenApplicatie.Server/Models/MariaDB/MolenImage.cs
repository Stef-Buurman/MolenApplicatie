using MolenApplicatie.Server.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MolenApplicatie.Server.Models.MariaDB
{
    [Table("molen_image")]
    public class MolenImage : DefaultModel, IEquatable<MolenImage>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public required string FilePath { get; set; }
        public required string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public string? Description { get; set; }
        public required string ExternalUrl { get; set; }
        public MolenData MolenData { get; set; } = null!;
        public Guid MolenDataId { get; set; }
        public bool IsAddedImage = false;

        public bool Equals(MolenImage? other)
        {
            if (other == null) return false;
            return FilePath == other.FilePath;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MolenImage other)
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