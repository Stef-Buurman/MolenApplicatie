using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenMakerService : DBDefaultService<MolenMaker>
    {
        private readonly MolenDbContext _context;
        public DBMolenMakerService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(MolenMaker molenMaker, out MolenMaker? existing)
        {
            return Exists(e => e.Name == molenMaker.Name && e.Year == molenMaker.Year, out existing);
        }
        public async Task<List<MolenMaker>> GetMakersOfMolen(int MolenId)
        {
            var makers = await _context.MolenMakers
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return makers;
        }
    }
}
