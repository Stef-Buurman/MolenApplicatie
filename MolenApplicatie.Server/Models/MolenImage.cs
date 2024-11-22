namespace MolenApplicatie.Models
{
    public class MolenImage
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }
        public DateTime? DateTaken { get; set; }

        public MolenImage(string filePath, string name, bool canBeDeleted = false, DateTime? dateTaken = null)
        {
            FilePath = filePath;
            Name = name;
            CanBeDeleted = canBeDeleted;
            DateTaken = dateTaken;
        }
    }
}