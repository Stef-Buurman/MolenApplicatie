using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeService : DBDefaultService<MolenType>
    {
        private readonly MolenDbContext _context;
        private readonly DBMolenTypeAssociationService _dBMolenTypeAssociationService;
        public DBMolenTypeService(MolenDbContext context, DBMolenTypeAssociationService dBMolenTypeAssociationService) : base(context)
        {
            _context = context;
            _dBMolenTypeAssociationService = dBMolenTypeAssociationService;
        }
        public override bool Exists(MolenType molenType, out MolenType? existing)
        {
            return Exists(e => e.Name == molenType.Name, out existing);
        }
        public override async Task Delete(MolenType molenType)
        {
            MolenType? molenTypeToDetele = await GetById(molenType.Id);
            if (molenTypeToDetele != null)
            {
                List<MolenTypeAssociation> molenTypeAssociations = _context.MolenTypeAssociations.Where(e => e.MolenTypeId == molenTypeToDetele.Id).ToList();
                await _dBMolenTypeAssociationService.DeleteRange(molenTypeAssociations);
                _context.MolenTypes.Remove(molenTypeToDetele);
            }
        }
    }
}
