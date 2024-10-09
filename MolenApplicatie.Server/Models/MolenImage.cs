using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenImage
    {
        [PrimaryKey, AutoIncrement]
        public int MolenImageId { get; set; }
        public int MolenDataId { get; set; }
        public byte[] Image { get; set; }
    }
}
