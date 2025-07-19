using MolenApplicatie.Server.Models;
using System.Text.Json;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using MolenApplicatie.Server.Services.Database;
using System.Globalization;

namespace MolenApplicatie.Server.Services
{
    public class PlacesService
    {
        private readonly HttpClient _client;
        private readonly MolenDbContext _dbContext;
        private readonly DBPlaceService _dBPlaceService;
        private readonly PlaceTypeService _placeTypeService;

        public PlacesService(MolenDbContext dbContext, DBPlaceService dBPlaceService, PlaceTypeService placeTypeService)
        {
            _client = new HttpClient();
            _dbContext = dbContext;
            _dBPlaceService = dBPlaceService;
            _placeTypeService = placeTypeService;
        }

        List<string> provinces = new List<string>
        {
            "Drenthe",
            "Flevoland",
            "Friesland",
            "Fryslân",
            "Gelderland",
            "Groningen",
            "Limburg",
            "North Brabant",
            "Noord-Brabant",
            "North Holland",
            "Noord-Holland",
            "Overijssel",
            "South Holland",
            "Zuid-Holland",
            "Utrecht",
            "Zeeland"
        };

        List<string> PlaceOptions = new List<string>
        {
          "capital of a political entity",
          "seat of government of a political entity",
          "populated place",
          "seat of a first-order administrative division",
          "seat of a second-order administrative division",
          "section of populated place",
          "populated locality",
          "populated places",
          "first-order administrative division",
          "second-order administrative division",
          "administrative division",
          "historical second-order administrative division",
          "section of independent political entity",
          "third-order administrative division"
        };

        List<string> featureClasses = new List<string>
        {
            //"",
            "H",
            "L",
            "P",
            "S",
            "R",
            "T",
            "U",
            "V"
        };

        public async Task test()
        {
            string jsonString = await File.ReadAllTextAsync("Json/AllePlace.json");
            List<Place> places = JsonSerializer.Deserialize<List<Place>>(jsonString)!;
            var startTime = DateTime.Now;
            Console.WriteLine("Saving progress...");
            await _dBPlaceService.AddOrUpdateRange(places, default, Enums.UpdateStrategy.Ignore);
            var midTime = DateTime.Now;
            //var changes = await _dbContext.SaveChangesAsync();
            var endTime = DateTime.Now;

            Console.WriteLine($"Saved {places.Count} molens in {midTime - startTime} seconds. Total time: {endTime - startTime} seconds.");
        }

        public async Task test2()
        {
            var data = await _dBPlaceService.GetAllAsync();
            File.WriteAllText("Json/AllePlace.json", JsonSerializer.Serialize(RemoveCircularDependencyAll(data), new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        public static List<Place>? RemoveCircularDependencyAll(List<Place>? places)
        {
            if (places == null) return null;
            places.ForEach(molen => RemoveCircularDependency(molen));
            return places;
        }

        public static Place? RemoveCircularDependency(Place? place)
        {
            if (place == null) return null;
            if (place.Type != null)
            {
                place.Type.Places = null;
            }
            return place;
        }

        public async Task<List<Place>> ReadAllNetherlandsPlaces()
        {
            int maxRows = 1000;
            int startRow = 0;
            int totalResults = -1;
            List<Place> places = new List<Place>();
            Dictionary<string, List<GeoName>> x = new Dictionary<string, List<GeoName>>();
            Dictionary<string, Dictionary<string, List<GeoName>>> y = new Dictionary<string, Dictionary<string, List<GeoName>>>();
            List<string> provincies = new List<string>();

            var responseProvincies = await _client.GetAsync($"http://api.geonames.org/searchJSON?country=NL&maxRows={maxRows}&featureClass=A&continentCode=&username=weetikveel12321&startRow={startRow}&q=first-order+administrative+division&lang=local");
            responseProvincies.EnsureSuccessStatusCode();

            var jsonResponseProvincies = await responseProvincies.Content.ReadAsStringAsync();

            var placesResponseProvincies = JsonSerializer.Deserialize<GeoNamesResponse>(jsonResponseProvincies, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (placesResponseProvincies != null && placesResponseProvincies.Geonames != null)
            {
                List<string> placesFound = placesResponseProvincies.Geonames.Select(geoName => geoName.ToponymName).ToList();
                provincies.AddRange(placesFound);
            }

            foreach (var featureClass in featureClasses)
            {
                startRow = 0;
                totalResults = -1;
                while (startRow <= 5000 || totalResults == -1)
                {
                    var response = await _client.GetAsync($"http://api.geonames.org/searchJSON?country=NL&maxRows={maxRows}&featureClass=P&continentCode=&username=weetikveel12321&startRow={startRow}&lang=local");
                    response.EnsureSuccessStatusCode();
                    if (!response.IsSuccessStatusCode)
                    {
                        break;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var placesResponse = JsonSerializer.Deserialize<GeoNamesResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (placesResponse != null)
                    {
                        if (placesResponse.Geonames == null) break;
                        if (totalResults == -1) totalResults = placesResponse.TotalResultsCount;

                        List<Place> placesFound = placesResponse.Geonames
                            .Where(geoName => _placeTypeService.GetPlaceType(geoName) != null)
                            .Select(geoName => new Place
                            {
                                Name = geoName.Name,
                                Province = geoName.Province,
                                Latitude = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                                Longitude = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                                Population = geoName.Population,
                                Country = geoName.CountryName,
                                Type = _placeTypeService.GetPlaceType(geoName)!
                            })
                            .ToList();

                        if (placesFound != null) places.AddRange(placesFound);
                        startRow += maxRows;
                    }
                }
            }

            foreach (var province in provincies)
            {
                foreach (var featureClass in featureClasses)
                {
                    startRow = 0;
                    int totalResultsProvince = -1;

                    while (startRow <= 5000 || (totalResultsProvince == -1 || startRow <= totalResultsProvince))
                    {
                        var response = await _client.GetAsync($"http://api.geonames.org/searchJSON?country=NL&maxRows={maxRows}&featureClass={featureClass}&continentCode=&username=weetikveel12321&startRow={startRow}&q={province}&lang=local");
                        response.EnsureSuccessStatusCode();
                        if (!response.IsSuccessStatusCode)
                        {
                            break;
                        }

                        var jsonResponse = await response.Content.ReadAsStringAsync();

                        var placesResponse = JsonSerializer.Deserialize<GeoNamesResponse>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (placesResponse != null)
                        {
                            if (placesResponse.Geonames == null) break;
                            if (totalResultsProvince == -1) totalResultsProvince = placesResponse.TotalResultsCount;

                            List<Place> placesFound = placesResponse.Geonames
                            .Where(geoName => _placeTypeService.GetPlaceType(geoName) != null)
                            .Select(geoName => new Place
                            {
                                Name = geoName.Name,
                                Province = geoName.Province,
                                Latitude = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                                Longitude = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                                Population = geoName.Population,
                                Country = geoName.CountryName,
                                Type = _placeTypeService.GetPlaceType(geoName)
                            })
                            .ToList();

                            if (placesFound != null) places.AddRange(placesFound);
                            startRow += maxRows;
                        }
                    }
                }
            }
            return await AddPlacesToMariaDb(places);
        }

        public async Task<Place?> GetPlaceByName(string name)
        {
            _dBPlaceService._cache.Exists(p => p.Name.ToLower() == name.ToLower(), out var foundPlace);
            if (foundPlace != null) return foundPlace;
            return await _dbContext.Places
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
        }

        public async Task<List<Place>> GetPlacesByName(string name)
        {
            var normalizedName = name.ToLower();

            return await _dbContext.Places
                .Where(p => p.Name.ToLower().Contains(normalizedName))
                .ToListAsync();
        }
        public async Task<Place?> GetPlaceById(Guid id) => await _dbContext.Places
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Place>> GetAllNetherlandsPlaces() => await _dbContext.Places
            .AsNoTracking()
            .ToListAsync();

        public async Task<List<Place>> AddPlacesToMariaDb(List<Place> Places)
        {
            HashSet<string> alreadyProcessed = new(StringComparer.OrdinalIgnoreCase);
            List<Place> placesInDB = await _dbContext.Places.ToListAsync();

            int x = 2500;

            for (int i = 0; i < Places.Count; i += x)
            {
                var batch = Places.Skip(i).Take(x).ToList();
                if (batch.Count > 0)
                {
                    await _dBPlaceService.AddOrUpdateRange(batch);
                    await _dbContext.SaveChangesAsync();
                }
            }
            await _dbContext.SaveChangesAsync();
            return await _dbContext.Places.ToListAsync();
        }
    }
}