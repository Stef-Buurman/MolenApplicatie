using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeAssociationService : DBDefaultService<MolenTypeAssociation>
    {
        private readonly MolenDbContext _context;
        public DBMolenTypeAssociationService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(MolenTypeAssociation molenTypeAssociation, out MolenTypeAssociation? existing)
        {
            return Exists(e => e.MolenDataId == molenTypeAssociation.MolenDataId && e.MolenTypeId == molenTypeAssociation.MolenTypeId, out existing);
        }
        public async Task<List<MolenTypeAssociation>> GetMolenTypeAssociationsOfMolen(int MolenId)
        {
            var molenTypeAssociations = await _context.MolenTypeAssociations
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return molenTypeAssociations;
        }
    }
}
