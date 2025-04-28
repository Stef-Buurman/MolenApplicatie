using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenTypeOld
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
