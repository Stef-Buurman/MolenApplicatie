using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Models
{
    public class UploadDeleteImageReturnType
    {
        public required MolenData Molen { get; set; }
        public required MapData MapData { get; set; }
    }
}
