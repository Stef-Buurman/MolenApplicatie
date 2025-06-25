using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            return Exists(e => e.Ten_Brugge_Nr == molenTBN.Ten_Brugge_Nr, out existing);
        }
        public override async Task<MolenTBN> Add(MolenTBN molenType)
        {
            if (molenType == null) return null;
            if (Exists(molenType, out MolenTBN? existingEntity))
            {
                return existingEntity;
            }
            return await base.Add(molenType);
        }
    }
}
