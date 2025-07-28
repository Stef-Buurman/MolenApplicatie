using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBPlaceService : DBDefaultService<Place>
    {
        private readonly DBPlaceTypeService _dBPlaceTypeService;
        public DBPlaceService(MolenDbContext context, DBPlaceTypeService dBPlaceTypeService) : base(context)
        {
            _dBPlaceTypeService = dBPlaceTypeService;
        }


        public override bool Exists(Place place, out Place? existing)
        {
            return Exists(e => place.Name == e.Name && place.Province == e.Province && place.Latitude == e.Latitude && place.Longitude == e.Longitude, out existing);
        }

        public override bool ExistsRange(List<Place> entities,
            out List<Place> matchingEntities,
            out List<Place> newEntities,
            out List<Place> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            return ExistsRange(
                entities,
                e => new { e.Name, e.Province, e.Latitude, e.Longitude },
                y => e => e.Name == y.Name && e.Province == y.Province && e.Latitude == y.Latitude && e.Longitude == y.Longitude,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB,
                token,
                strat
            );
        }

        public override async Task<List<Place>> AddOrUpdateRange(List<Place> entities, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entities == null)
                return entities;

            var all_types = entities.Select(e => e.Type).Where(t => t != null).Distinct().ToList();
            if (all_types.Count > 0)
            {
                all_types = await _dBPlaceTypeService.AddOrUpdateRange(all_types, token, strat);
            }

            var all = await _cache.GetAllAsync();
            var entitiesToAdd = new List<Place>();
            var entitiesToUpdate = new List<Place>();

            foreach (Place entity in entities)
            {
                if (entity.Type != null)
                {
                    entity.Type = all_types.FirstOrDefault(t => entity.Type.Equals(t));
                }
                if (entity.Type != null && entity.Type.Id != Guid.Empty)
                {
                    entity.PlaceTypeId = entity.Type.Id;
                    entity.Type = null;
                }
            }

            if (ExistsRange(entities, out List<Place> existingEntities, out List<Place> newEntities, out List<Place> updatedEntities, false, token, strat))
            {
                entitiesToAdd.AddRange(newEntities);
                entitiesToUpdate.AddRange(updatedEntities);
            }
            else
            {
                entitiesToAdd.AddRange(entities);
            }


            await AddRangeAsync(entitiesToAdd, token);
            await UpdateRange(entitiesToUpdate, token, strat);

            return entities;
        }

        public override async Task<Place> AddOrUpdate(Place place, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (place == null) return null;
            if (Exists(place, out Place? existingEntity))
            {
                if (strat == UpdateStrategy.Ignore)
                    return existingEntity;

                if (strat == UpdateStrategy.Patch)
                    PatchUtil.Patch(place, existingEntity);

                place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type, token, strat);
                _context.Entry(existingEntity).CurrentValues.SetValues(place);
                return existingEntity;
            }
            return await Add(place, token);
        }
        public override async Task<Place> Add(Place place, CancellationToken token = default)
        {
            if (place == null) return null;
            if (Exists(place, out Place? existingEntity))
            {
                return existingEntity;
            }
            if (place.Type != null)
            {
                place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type, token);
            }
            return await base.Add(place, token);
        }

        public override async Task<Place> Update(Place place, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (place == null) return null;
            place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type, token, strat);
            return await base.Update(place, token, strat);
        }
    }
}
