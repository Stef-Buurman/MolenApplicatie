using SQLite;

namespace MolenApplicatie.Server.Models
{
    public class MolenMaker
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Year { get; set; }
        public int MolenDataId { get; set; }
    }
}
