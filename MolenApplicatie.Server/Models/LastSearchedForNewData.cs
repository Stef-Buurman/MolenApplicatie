using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class LastSearchedForNewData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime LastSearched { get; set; }
    }
}
