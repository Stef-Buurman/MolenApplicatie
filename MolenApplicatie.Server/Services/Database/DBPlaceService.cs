using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBPlaceService : DBDefaultService<Place>
    {
        private readonly MolenDbContext _context;
        public DBPlaceService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(Place place, out Place? existing)
        {
            existing = _context.Places.FirstOrDefault(e => e.Name == place.Name);
            return existing != null;
        }
    }
}
