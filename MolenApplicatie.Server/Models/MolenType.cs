using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenType
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
