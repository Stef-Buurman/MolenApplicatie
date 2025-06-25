using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBPlaceService : DBDefaultService<Place>
    {
        public DBPlaceService(MolenDbContext context) : base(context)
        {}
        public override bool Exists(Place place, out Place? existing)
        {
            return Exists(e => e.Name == place.Name, out existing);
        }
    }
}
