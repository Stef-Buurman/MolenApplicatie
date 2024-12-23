using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class AddedImage
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public DateTime? DateTaken { get; set; }
        public string Description { get; set; }
        public int MolenDataId { get; set; }
        public bool IsAddedImage { get; set; } = true;
    }
}
