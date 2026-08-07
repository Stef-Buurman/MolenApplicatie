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

            entities = entities.Where(entity => entity != null).ToList();
            token.ThrowIfCancellationRequested();

            foreach (var entity in entities)
            {
                if (entity.MolenData != null && entity.MolenData.Id != Guid.Empty) entity.MolenDataId = entity.MolenData.Id;
            }

            var requestedTypesByName = entities
                .Select(entity => entity.MolenType)
                .Where(type =>
                    type != null &&
                    !string.IsNullOrWhiteSpace(type.Name))
                .Cast<MolenType>()
                .GroupBy(
                    type => type.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            var databaseTypes = await _context.MolenTypes.AsNoTracking().ToListAsync(token);

            var typesByName = _context.MolenTypes.Local
                .Concat(databaseTypes)
                .Where(type => !string.IsNullOrWhiteSpace(type.Name))
                .GroupBy(
                    type => type.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            var newTypes = requestedTypesByName.Where(pair => !typesByName.ContainsKey(pair.Key)).Select(pair => new MolenType { Name = pair.Value.Name.Trim() }).ToList();

            if (newTypes.Count > 0)
            {
                await _dBMolenTypeService.AddRangeAsync(newTypes, token);
                foreach (var newType in newTypes) typesByName[newType.Name.Trim()] = newType;
            }

            /*
             * Resolve every association to a MolenTypeId and then remove both
             * navigation properties. EF should only track the association itself.
             */
            foreach (var entity in entities)
            {
                if (entity.MolenType != null)
                {
                    var typeName = entity.MolenType.Name?.Trim();

                    if (!string.IsNullOrWhiteSpace(typeName) &&
                        typesByName.TryGetValue(
                            typeName,
                            out var resolvedType))
                    {
                        entity.MolenTypeId = resolvedType.Id;
                    }
                    else if (entity.MolenType.Id != Guid.Empty)
                    {
                        entity.MolenTypeId = entity.MolenType.Id;
                    }
                }

                entity.MolenType = null!;
                entity.MolenData = null!;
            }

            var invalidAssociation = entities.FirstOrDefault(entity => entity.MolenDataId == Guid.Empty || entity.MolenTypeId == Guid.Empty);
            if (invalidAssociation != null)
                throw new InvalidOperationException("A MolenTypeAssociation has no valid MolenDataId or MolenTypeId.");


            var molenDataIds = entities.Select(entity => entity.MolenDataId).Distinct().ToList();

            var molenTypeIds = entities.Select(entity => entity.MolenTypeId).Distinct().ToList();

            var databaseAssociations = await _context.MolenTypeAssociations
                .AsNoTracking()
                .Where(association =>
                    molenDataIds.Contains(association.MolenDataId) &&
                    molenTypeIds.Contains(association.MolenTypeId))
                .ToListAsync(token);

            var associationsByKey = _context.MolenTypeAssociations.Local
                .Where(association =>
                    molenDataIds.Contains(association.MolenDataId) &&
                    molenTypeIds.Contains(association.MolenTypeId))
                .Concat(databaseAssociations)
                .GroupBy(association => (
                    association.MolenDataId,
                    association.MolenTypeId
                ))
                .ToDictionary(
                    group => group.Key,
                    group => group.First()
                );

            var newAssociations = entities.GroupBy(entity => (entity.MolenDataId, entity.MolenTypeId)).Where(group => !associationsByKey.ContainsKey(group.Key)).Select(group => group.First()).ToList();

            if (newAssociations.Count > 0)
            {
                await base.AddRangeAsync(newAssociations, token);

                foreach (var newAssociation in newAssociations)
                    associationsByKey[(newAssociation.MolenDataId, newAssociation.MolenTypeId)] = newAssociation;
            }

            foreach (var entity in entities)
            {
                if (associationsByKey.TryGetValue((entity.MolenDataId, entity.MolenTypeId), out var resolvedAssociation))
                    entity.Id = resolvedAssociation.Id;
            }

            return entities;
        }
    }
}
