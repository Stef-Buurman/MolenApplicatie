using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenMakerService : DBDefaultService<MolenMaker>
    {
        public DBMolenMakerService(MolenDbContext context) : base(context)
        {}
        public override bool Exists(MolenMaker molenMaker, out MolenMaker? existing)
        {
            return Exists(e => e.Name == molenMaker.Name && e.Year == molenMaker.Year, out existing);
        }

        public override bool ExistsRange(List<MolenMaker> entities, out List<MolenMaker> matchingEntities, out List<MolenMaker> newEntities, out List<MolenMaker> updatedEntities, bool searchDB)
        {
            return ExistsRange(
                entities,
                e => new { e.Name, e.Year },
                y => e => e.Name == y.Name && e.Year == y.Year,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB
            );
        }

        public async Task<List<MolenMaker>> GetMakersOfMolen(Guid MolenId)
        {
            var makers = await _context.MolenMakers
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return makers;
        }
    }
}
