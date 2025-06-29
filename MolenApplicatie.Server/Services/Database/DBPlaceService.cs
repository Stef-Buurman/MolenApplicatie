using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBPlaceService : DBDefaultService<Place>
    {
        private readonly MolenDbContext _context;
        private readonly DBPlaceTypeService _dBPlaceTypeService;
        public DBPlaceService(MolenDbContext context, DBPlaceTypeService dBPlaceTypeService)
            : base(context)
        {
            _context = context;
            _dBPlaceTypeService = dBPlaceTypeService;
        }

        public override bool Exists(Place place, out Place? existing)
        {
            return Exists(e => place.Name == e.Name && place.Province == e.Province && place.Latitude == e.Latitude && place.Longitude == e.Longitude, out existing);
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
                if (entity.Type != null && entity.Type.Id > 0)
                {
                    entity.PlaceTypeId = entity.Type.Id;
                    entity.Type = null;
                }
                var existingEntity = all.FirstOrDefault(e => e.Equals(entity));
                if (existingEntity != null)
                {
                    if (entity.Id == 0 && existingEntity.Id != 0)
                        entity.Id = existingEntity.Id;

                    existingEntity.Type = null;
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
