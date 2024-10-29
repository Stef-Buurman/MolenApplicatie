namespace MolenApplicatie.Models
{
    public class MolenImage
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }

        public MolenImage(string filePath, string name, bool canBeDeleted = false)
        {
            FilePath = filePath;
            Name = name;
            CanBeDeleted = canBeDeleted;
        }
    }

    public class MolenImage2
    {
        public string FilePath { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }

        public MolenImage2(string filePath, string name, string url, bool canBeDeleted = false)
        {
            FilePath = filePath;
            Url = url;
            Name = name;
            CanBeDeleted = canBeDeleted;
        }
    }


}