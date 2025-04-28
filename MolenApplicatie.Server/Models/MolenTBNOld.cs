using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenTBNOld
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Ten_Brugge_Nr { get; set; }
    }
}
