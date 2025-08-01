using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
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

        public override bool ExistsRange(List<MolenType> entities,
            out List<MolenType> matchingEntities,
            out List<MolenType> newEntities,
            out List<MolenType> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => e.Name.ToLower(),
                y => e => e.Name.ToLower() == y.Name.ToLower(),
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }
    }
}
