namespace MolenApplicatie.Models
{
    public class MolenImage
    {
        public byte[] Content { get; set; }
        public string Name { get; set; }
        public bool CanBeDeleted { get; set; }

        public MolenImage(byte[] image, string name, bool canBeDeleted = false)
        {
            Content = image;
            Name = name;
            CanBeDeleted = canBeDeleted;
        }
    }
}