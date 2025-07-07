using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenDissappearedYearsService : DBDefaultService<DisappearedYearInfo>
    {
        private readonly MolenDbContext _context;
        public DBMolenDissappearedYearsService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(DisappearedYearInfo molenDissappearedYears, out DisappearedYearInfo? existing)
        {
            return Exists(e => e.MolenDataId == molenDissappearedYears.MolenDataId && e.Year == molenDissappearedYears.Year, out existing);
        }
        public async Task<List<DisappearedYearInfo>> GetDissappearedYearsOfMolen(Guid MolenId)
        {
            var years = await _context.DisappearedYearInfos
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return years;
        }
    }
}
