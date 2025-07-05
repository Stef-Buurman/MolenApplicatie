using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBPlaceTypeService : DBDefaultService<PlaceType>
    {
        public DBPlaceTypeService(MolenDbContext context) : base(context) { }

        public override bool Exists(PlaceType type, out PlaceType? existing)
        {
            return Exists(e => e.Name == type.Name && e.Group == type.Group, out existing);
        }
    }
}
