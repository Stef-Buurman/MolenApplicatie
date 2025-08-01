using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenDissappearedYearsService : DBDefaultService<DisappearedYearInfo>
    {
        public DBMolenDissappearedYearsService(MolenDbContext context) : base(context)
        { }

        public override bool Exists(DisappearedYearInfo molenDissappearedYears, out DisappearedYearInfo? existing)
        {
            return Exists(e => e.MolenDataId == molenDissappearedYears.MolenDataId && e.Year == molenDissappearedYears.Year, out existing);
        }

        public override bool ExistsRange(List<DisappearedYearInfo> entities,
            out List<DisappearedYearInfo> matchingEntities,
            out List<DisappearedYearInfo> newEntities,
            out List<DisappearedYearInfo> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => new { e.MolenDataId, e.Year },
                y => e => e.MolenDataId == y.MolenDataId && e.Year == y.Year,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }

        public async Task<List<DisappearedYearInfo>> GetDissappearedYearsOfMolen(Guid MolenId)
        {
            var years = await _context.DisappearedYearInfos
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return years;
        }

        public async Task<Dictionary<Guid, List<DisappearedYearInfo>>> GetDissappearedYearsOfMolens(List<Guid> molens)
        {
            var years = await _context.DisappearedYearInfos
                .Where(e => molens.Contains(e.MolenDataId))
                .GroupBy(e => e.MolenDataId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());
            return years;
        }
    }
}
