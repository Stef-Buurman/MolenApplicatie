using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBMolenTypeAssociationService : DBDefaultService<MolenTypeAssociation>
    {
        private readonly DBMolenTypeService _dBMolenTypeService;
        public DBMolenTypeAssociationService(MolenDbContext context, DBMolenTypeService dBMolenTypeService) : base(context)
        {
            _dBMolenTypeService = dBMolenTypeService;
        }

        public override bool Exists(MolenTypeAssociation molenTypeAssociation, out MolenTypeAssociation? existing)
        {
            return Exists(e => e.MolenTypeId == molenTypeAssociation.MolenTypeId && e.MolenDataId == molenTypeAssociation.MolenDataId, out existing);
        }

        public override bool ExistsRange(List<MolenTypeAssociation> entities,
            out List<MolenTypeAssociation> matchingEntities,
            out List<MolenTypeAssociation> newEntities,
            out List<MolenTypeAssociation> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => new { e.MolenDataId, e.MolenTypeId },
                y => e => e.MolenDataId == y.MolenDataId && e.MolenTypeId == y.MolenTypeId,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }

        public async Task<List<MolenTypeAssociation>> GetMolenTypeAssociationsOfMolen(Guid MolenId)
        {
            var molenTypeAssociations = await _context.MolenTypeAssociations
                .Where(e => e.MolenDataId == MolenId)
                .ToListAsync();
            return molenTypeAssociations;
        }

        public async Task<Dictionary<Guid, List<MolenTypeAssociation>>> GetMolenTypeAssociationsOfMolens(List<Guid> molens)
        {
            var molenTypeAssociations = await _context.MolenTypeAssociations
                .Where(e => molens.Contains(e.MolenDataId))
                .GroupBy(e => e.MolenDataId)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());
            return molenTypeAssociations;
        }

        public override async Task<MolenTypeAssociation> Add(MolenTypeAssociation molenTypeAssociation, CancellationToken token = default)
        {
            if (molenTypeAssociation == null) return molenTypeAssociation;
            if (molenTypeAssociation.MolenData != null)
            {
                molenTypeAssociation.MolenDataId = molenTypeAssociation.MolenData.Id;
                molenTypeAssociation.MolenData = null!;
            }
            molenTypeAssociation.MolenType = await _dBMolenTypeService.AddOrUpdate(molenTypeAssociation.MolenType);
            molenTypeAssociation.MolenTypeId = molenTypeAssociation.MolenType.Id;
            molenTypeAssociation.MolenType = null;
            return await base.Add(molenTypeAssociation, token);
        }

        public override async Task<MolenTypeAssociation> Update(MolenTypeAssociation molenTypeAssociation, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (molenTypeAssociation == null) return molenTypeAssociation;
            if (molenTypeAssociation.MolenData != null)
            {
                molenTypeAssociation.MolenDataId = molenTypeAssociation.MolenData.Id;
                molenTypeAssociation.MolenData = null!;
            }
            molenTypeAssociation.MolenType = await _dBMolenTypeService.AddOrUpdate(molenTypeAssociation.MolenType);
            molenTypeAssociation.MolenTypeId = molenTypeAssociation.MolenType.Id;
            molenTypeAssociation.MolenType = null!;
            return await base.Update(molenTypeAssociation, token, strat);
        }
        public override async Task<List<MolenTypeAssociation>> AddOrUpdateRange(List<MolenTypeAssociation> entities, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entities == null || entities.Count == 0)
                return entities;

            var all_types = entities.Select(e => e.MolenType).Where(t => t != null).Distinct().ToList();
            if (all_types.Count > 0)
            {
                all_types = await _dBMolenTypeService.AddOrUpdateRange(all_types, token, strat);
            }

            await _cache.GetAllAsync();

            var entitiesToAdd = new List<MolenTypeAssociation>();
            var entitiesToUpdate = new List<MolenTypeAssociation>();

            foreach (var entity in entities)
            {
                if (entity.MolenType != null)
                {
                    entity.MolenType = all_types.FirstOrDefault(t => entity.MolenType.Equals(t));
                }

                if (entity.MolenType != null && entity.MolenType.Id != Guid.Empty)
                {
                    entity.MolenTypeId = entity.MolenType.Id;
                }
                entity.MolenType = null;

                if (entity.MolenData != null && entity.MolenData.Id != Guid.Empty)
                {
                    entity.MolenDataId = entity.MolenData.Id;
                }
                entity.MolenData = null;
            }

            if (ExistsRange(entities, out List<MolenTypeAssociation> existingEntities, out List<MolenTypeAssociation> newEntities, out List<MolenTypeAssociation> updatedEntities, false, token, strat))
            {
                entitiesToAdd.AddRange(newEntities);
                entitiesToUpdate.AddRange(updatedEntities);
            }
            else
            {
                entitiesToAdd.AddRange(entities);
            }

            if (entitiesToAdd.Count > 0) await AddRangeAsync(entitiesToAdd, token);
            if (entitiesToUpdate.Count > 0) await UpdateRange(entitiesToUpdate, token, strat);

            return entities;
        }
    }
}
