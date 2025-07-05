using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeService : DBDefaultService<MolenType>
    {
        public DBMolenTypeService(MolenDbContext context) : base(context) { }

        public override bool Exists(MolenType molenType, out MolenType? existing)
        {
            return Exists(e => e.Name.ToLower() == molenType.Name.ToLower(), out existing);
        }
    }
}
