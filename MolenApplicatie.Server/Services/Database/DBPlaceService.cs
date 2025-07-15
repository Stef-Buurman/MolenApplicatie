using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

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

        public override bool ExistsRange(List<Place> entities, out List<Place> matchingEntities, out List<Place> newEntities, out List<Place> updatedEntities, bool searchDB = true)
        {
            return ExistsRange(
                entities,
                e => new { e.Name, e.Province, e.Latitude, e.Longitude },
                y => e => e.Name == y.Name && e.Province == y.Province && e.Latitude == y.Latitude && e.Longitude == y.Longitude,
                out matchingEntities,
                out newEntities,
                out updatedEntities,
                searchDB
            );
        }

        public override async Task<List<Place>> AddOrUpdateRange(List<Place> entities)
        {
            if (entities == null)
                return entities;

            var all_types = entities.Select(e => e.Type).Where(t => t != null).Distinct().ToList();
            if (all_types.Count > 0)
            {
                all_types = await _dBPlaceTypeService.AddOrUpdateRange(all_types);
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
                var existingEntity = all.FirstOrDefault(e => e.Equals(entity));
                if (existingEntity != null)
                {
                    if (entity.Id == Guid.Empty && existingEntity.Id != Guid.Empty)
                        entity.Id = existingEntity.Id;

                    existingEntity.Type = null;
                    _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    _cache.Update(entity);
                }
                else
                {
                    entitiesToAdd.Add(entity);
                }
            }

            await AddRangeAsync(entitiesToAdd);

            return entities;
        }

        public override async Task<Place> AddOrUpdate(Place place)
        {
            if (place == null) return null;
            Place? existingEntity = await GetById(place.Id);
            if (existingEntity != null)
            {
                place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type);
                _context.Entry(existingEntity).CurrentValues.SetValues(place);
                return existingEntity;
            }
            return await Add(place);
        }
        public override async Task<Place> Add(Place place)
        {
            if (place == null) return null;
            if (Exists(place, out Place? existingEntity))
            {
                return existingEntity;
            }
            if (place.Type != null)
            {
                place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type);
            }
            return await base.Add(place);
        }

        public override async Task<Place> Update(Place place)
        {
            if (place == null) return null;
            Place? existingEntity = await GetById(place.Id);
            place.Type = await _dBPlaceTypeService.AddOrUpdate(place.Type);
            return await base.Update(place);
        }
    }
}
