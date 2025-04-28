using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class LastSearchedForNewDataOld
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime LastSearched { get; set; }
    }
}
