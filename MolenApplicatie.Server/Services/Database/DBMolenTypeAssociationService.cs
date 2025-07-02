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
            molenTypeAssociation.MolenType = null;
            return await base.Add(molenTypeAssociation);
        }

        public override async Task<MolenTypeAssociation> Update(MolenTypeAssociation molenTypeAssociation)
        {
            if (molenTypeAssociation == null) return molenTypeAssociation;
            molenTypeAssociation.MolenDataId = molenTypeAssociation.MolenData.Id;
            molenTypeAssociation.MolenData = null!;
            molenTypeAssociation.MolenType = await _dBMolenTypeService.AddOrUpdate(molenTypeAssociation.MolenType);
            molenTypeAssociation.MolenTypeId = molenTypeAssociation.MolenType.Id;
            molenTypeAssociation.MolenType = null;
            return await base.Update(molenTypeAssociation);
        }

        public override async Task<List<MolenTypeAssociation>> AddOrUpdateRange(List<MolenTypeAssociation> entities)
        {
            if (entities == null)
                return entities;

            var all_types = entities.Select(e => e.MolenType).Where(t => t != null).Distinct().ToList();
            if (all_types.Count > 0)
            {
                all_types = await _dBMolenTypeService.AddOrUpdateRange(all_types);
            }

            var entitiesToAdd = new List<MolenTypeAssociation>();
            var entitiesToUpdate = new List<MolenTypeAssociation>();

            foreach (MolenTypeAssociation entity in entities)
            {
                if (entity.MolenType != null)
                {
                    entity.MolenType = all_types.FirstOrDefault(t => entity.MolenType.Equals(t));
                }
                if (entity.MolenType != null && entity.MolenType.Id > 0)
                {
                    entity.MolenTypeId = entity.MolenType.Id;
                    entity.MolenType = null;
                }
                if (Exists(entity, out var existingEntity))
                {
                    if (entity.Id == 0 && existingEntity.Id != 0)
                        entity.Id = existingEntity.Id;

                    existingEntity.MolenType = null;
                    _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                }
                else
                {
                    entitiesToAdd.Add(entity);
                }
            }

            await AddRangeAsync(entitiesToAdd);

            return entities;
        }
    }
}
