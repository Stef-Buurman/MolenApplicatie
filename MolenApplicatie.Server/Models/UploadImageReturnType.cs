using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Models
{
    public class UploadDeleteImageReturnType
    {
        public MolenData Molen { get; set; }
        public MapData MapData { get; set; }
    }
}
