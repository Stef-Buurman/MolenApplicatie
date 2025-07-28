using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
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

        public override bool ExistsRange(List<PlaceType> entities,
            out List<PlaceType> matchingEntities,
            out List<PlaceType> newEntities,
            out List<PlaceType> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => new { e.Name, e.Group },
                y => e => e.Name == y.Name && e.Group == y.Group,
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
