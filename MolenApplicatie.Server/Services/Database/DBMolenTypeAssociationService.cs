using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeAssociationService : DBDefaultService<MolenTypeAssociation>
    {
        private readonly MolenDbContext _context;
        private readonly DBMolenTypeService _dBMolenTypeService;
        public DBMolenTypeAssociationService(MolenDbContext context, DBMolenTypeService dBMolenTypeService) : base(context)
        {
            _context = context;
            _dBMolenTypeService = dBMolenTypeService;
        }
        public override bool Exists(MolenTypeAssociation molenTypeAssociation, out MolenTypeAssociation? existing)
        {
            return Exists(e => e.MolenTypeId == molenTypeAssociation.MolenTypeId && e.MolenDataId == molenTypeAssociation.MolenDataId, out existing);
        }
        public async Task<List<MolenTypeAssociation>> GetMolenTypeAssociationsOfMolen(int MolenId)
        {
            var molenTypeAssociations = await _context.MolenTypeAssociations
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return molenTypeAssociations;
        }

        public override async Task<MolenTypeAssociation> Add(MolenTypeAssociation molenTypeAssociation)
        {
            if (molenTypeAssociation == null) return molenTypeAssociation;
            molenTypeAssociation.MolenDataId = molenTypeAssociation.MolenData.Id;
            molenTypeAssociation.MolenData = null!;
            molenTypeAssociation.MolenType = await _dBMolenTypeService.AddOrUpdate(molenTypeAssociation.MolenType);
            molenTypeAssociation.MolenTypeId = molenTypeAssociation.MolenType.Id;
            return await base.Add(molenTypeAssociation);
        }

        public override async Task<MolenTypeAssociation> Update(MolenTypeAssociation molenTypeAssociation)
        {
            if (molenTypeAssociation == null) return molenTypeAssociation;
            molenTypeAssociation.MolenDataId = molenTypeAssociation.MolenData.Id;
            molenTypeAssociation.MolenData = null!;
            molenTypeAssociation.MolenType = await _dBMolenTypeService.AddOrUpdate(molenTypeAssociation.MolenType);
            molenTypeAssociation.MolenTypeId = molenTypeAssociation.MolenType.Id;
            return await base.Update(molenTypeAssociation);
        }
    }
}
