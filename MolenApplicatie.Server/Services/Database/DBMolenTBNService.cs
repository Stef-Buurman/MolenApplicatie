using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;
namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTBNService : DBDefaultService<MolenTBN>
    {
        public DBMolenTBNService(MolenDbContext context) : base(context)
        { }
        public override bool Exists(MolenTBN molenTBN, out MolenTBN? existing)
        {
            return Exists(e => e.Ten_Brugge_Nr == molenTBN.Ten_Brugge_Nr, out existing);
        }

        public override bool ExistsRange(List<MolenTBN> entities, out List<MolenTBN> matchingEntities, out List<MolenTBN> newEntities, out List<MolenTBN> updatedEntities, bool searchDB = true)
        {
            return ExistsRange(
                entities,
                e => e.Ten_Brugge_Nr,
                y => e => e.Ten_Brugge_Nr == y.Ten_Brugge_Nr,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB
            );
        }
    }
}
