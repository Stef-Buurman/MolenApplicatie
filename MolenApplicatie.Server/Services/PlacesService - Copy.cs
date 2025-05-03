using MolenApplicatie.Server.Models;
using System.Text.Json;
using System.Globalization;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MolenApplicatie.Server.Services
{
    public class PlacesService2_0
    {
        private readonly HttpClient _client;
        private readonly MolenDbContext _dbContext;

        public PlacesService2_0(MolenDbContext dbContext)
        {
            _client = new HttpClient();
            _dbContext = dbContext;
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

        public async Task<List<Place>> ReadAllNetherlandsPlaces()
        {
            int maxRows = 1000;
            int startRow = 0;
            int totalResults = -1;
            List<Place> places = new List<Place>();

            while (startRow < 5000 || totalResults == -1)
            {
                try
                {
                    var response = await _client.GetAsync($"http://api.geonames.org/searchJSON?country=NL&maxRows={maxRows}&featureClass=P&continentCode=&username=weetikveel12321&startRow={startRow}&lang=local");
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var placesResponse = JsonSerializer.Deserialize<GeoNamesResponse>(jsonResponse);
                    if (placesResponse != null && placesResponse.Geonames != null)
                    {
                        if (totalResults == -1) totalResults = placesResponse.TotalResultsCount;

                        List<Place>? placesFound = placesResponse.Geonames.Select(geoName => new Place
                        {
                            Name = geoName.Name,
                            Province = geoName.Province,
                            Latitude = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                            Longitude = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                            Population = geoName.Population
                        }).ToList();

                        if(placesFound != null) places.AddRange(placesFound);
                        startRow += maxRows;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return null;
                }
            }


            foreach (var province in provinces)
            {
                startRow = 0;
                int totalResultsProvince = -1;
                while (startRow < 5000 || totalResultsProvince == -1)
                {
                    try
                    {
                        var response = await _client.GetAsync($"http://api.geonames.org/searchJSON?country=NL&maxRows={maxRows}&featureClass=P&continentCode=&username=weetikveel12321&startRow={startRow}&q={province}&lang=local");
                        response.EnsureSuccessStatusCode();

                        var jsonResponse = await response.Content.ReadAsStringAsync();

                        var placesResponse = JsonSerializer.Deserialize<GeoNamesResponse>(jsonResponse);
                        if (placesResponse != null && placesResponse.Geonames != null)
                        {
                            if (totalResultsProvince == -1) totalResultsProvince = placesResponse.TotalResultsCount;
                            List<Place>? placesFound = placesResponse.Geonames.Select(geoName => new Place
                            {
                                Name = geoName.Name,
                                Province = geoName.Province,
                                Latitude = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                                Longitude = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                                Population = geoName.Population
                            }).ToList();

                            if (placesFound != null) places.AddRange(placesFound);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        return null;
                    }
                }
            }

            return await AddPlacesToMariaDb(places);
        }

        //public async Task<List<Place>> GetPlacesByProvince(string province)
        //{
        //    return await _db.QueryAsync<Place>($"SELECT * FROM Place WHERE Name LIKE '%{province}%'");
        //}

        public async Task<List<Place>> GetPlacesByProvince(string province) => await _dbContext.Places
            .Where(p => p.Province.Contains(province, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        //public async Task<List<Place>> GetPlacesByType(string type)
        //{
        //    return await _db.QueryAsync<Place>($"SELECT * FROM Place WHERE Name LIKE '%{type}%'");
        //}

        public async Task<List<Place>> GetPlacesByType(string type) => await _dbContext.Places
            .Where(p => p.Name.Contains(type, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        //public async Task<Place> GetPlaceByName(string name)
        //{
        //    return await _db.FindWithQueryAsync<Place>($"SELECT * FROM Place WHERE Name = '{name}'");
        //}

        public async Task<Place?> GetPlaceByName(string name) => await _dbContext.Places
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        //public async Task<Place> GetPlaceById(int id)
        //{
        //    return await _db.FindWithQueryAsync<Place>($"SELECT * FROM Place WHERE Id = {id}");
        //}

        public async Task<Place?> GetPlaceById(int id) => await _dbContext.Places
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        //public async Task<List<Place>> GetAllNetherlandsPlaces()
        //{
        //    return await _db.Table<Place>();
        //}

        public async Task<List<Place>> GetAllNetherlandsPlaces() => await _dbContext.Places
            .AsNoTracking()
            .ToListAsync();

        public async Task<List<Place>> AddPlacesToMariaDb(List<Place> Places)
        {
            HashSet<string> alreadyProcessed = new(StringComparer.OrdinalIgnoreCase);
            List<Place> placesToAdd = new();

            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            foreach (Place place in Places)
            {
                var normalizedName = place.Name.Trim();

                if (alreadyProcessed.Contains(normalizedName))
                    continue;

                alreadyProcessed.Add(normalizedName);

                Place? tracked = _dbContext.ChangeTracker.Entries<Place>()
                    .Select(e => e.Entity)
                    .FirstOrDefault(t => t.Name.Trim().Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

                Place? existingPlace = tracked;
                if (existingPlace == null)
                {
                    try
                    {
                        existingPlace = await _dbContext.Places
                            .AsNoTracking()
                            .FirstOrDefaultAsync(pl => pl.Name == normalizedName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DB Error] State: {connection.State}, Message: {ex.Message}");
                        throw;
                    }
                }

                if (existingPlace == null)
                {
                    placesToAdd.Add(place);
                }
            }

            if (placesToAdd.Any())
            {
                await _dbContext.Places.AddRangeAsync(placesToAdd);
                await _dbContext.SaveChangesAsync();
            }

            return await _dbContext.Places.ToListAsync();
        }

        public async Task<List<Place>> AddPlacesToMariaDb(List<PlaceOld> Places)
        {
            List<Place> place = Places.Select(Place => new Place()
            {
                Name = Place.Name.Trim(),
                Population = Place.Population,
                Province = Place.Province.Trim(),
                Latitude = Place.Lat,
                Longitude = Place.Lon
            }).ToList();

            return await AddPlacesToMariaDb(place);
        }
    }
}