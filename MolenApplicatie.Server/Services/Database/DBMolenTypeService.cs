using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeService : DBDefaultService<MolenType>
    {
        private readonly MolenDbContext _context;
        public DBMolenTypeService(MolenDbContext context) : base(context)
        {
            _context = context;
        }
        public override bool Exists(MolenType molenType, out MolenType? existing)
        {
            return Exists(e => e.Name.ToLower() == molenType.Name.ToLower(), out existing);
        }
        public override async Task Delete(MolenType molenType)
        {
            MolenType? molenTypeToDetele = await GetById(molenType.Id);
            if (molenTypeToDetele != null)
            {
                _context.MolenTypes.Remove(molenTypeToDetele);
            }
        }
        public override async Task<MolenType> Add(MolenType molenType)
        {
            if (molenType == null) return null;
            if (Exists(molenType, out MolenType? existingEntity))
            {
                return existingEntity;
            }
            return await base.Add(molenType);
        }
    }
}
