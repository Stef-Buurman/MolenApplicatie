using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenYearInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Status_before { get; set; }
        public int Year { get; set; }
        public string Status_after { get; set; }
        public int MolenDataId { get; set; }
    }
}
