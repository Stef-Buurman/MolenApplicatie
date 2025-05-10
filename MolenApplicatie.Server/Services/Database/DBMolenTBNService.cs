using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTBNService : DBDefaultService<MolenTBN>
    { 
        private readonly MolenDbContext _context;
        public DBMolenTBNService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(MolenTBN molenTBN, out MolenTBN? existing)
        {
            existing = _context.MolenTBNs.FirstOrDefault(e => e.Ten_Brugge_Nr == molenTBN.Ten_Brugge_Nr);
            return existing != null;
        }
    }
}
