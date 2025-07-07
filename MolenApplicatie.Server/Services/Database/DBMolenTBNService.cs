using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTBNService : DBDefaultService<MolenTBN>
    {
        public DBMolenTBNService(MolenDbContext context) : base(context)
        {}
        public override bool Exists(MolenTBN molenTBN, out MolenTBN? existing)
        {
            return Exists(e => e.Ten_Brugge_Nr == molenTBN.Ten_Brugge_Nr, out existing);
        }
    }
}
