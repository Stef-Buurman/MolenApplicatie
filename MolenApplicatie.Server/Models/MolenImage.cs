using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenImage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public string Description { get; set; }
        public string ExternalUrl { get; set; }
        public int MolenDataId { get; set; }
        public bool IsAddedImage { get; set; } = false;
    }
}