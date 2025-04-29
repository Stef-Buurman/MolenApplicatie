using MolenApplicatie.Server.Models;
using System.Text.Json;
using System.Globalization;
using MolenApplicatie.Server.Utils;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Microsoft.Extensions.Logging;
using System.Data;

namespace MolenApplicatie.Server.Services
{
    public class PlacesService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        private readonly HttpClient _client;
        private List<string> allowedTypes = new List<string>();

        private readonly DbConnection _db;
        private readonly MolenDbContext _dbContext;

        public PlacesService(MolenDbContext dbContext)
        {
            _client = new HttpClient();
            _db = new DbConnection(Globals.DBAlleMolens);
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

        public async Task<List<PlaceOld>> ReadAllNetherlandsPlaces()
        {
            int maxRows = 1000;
            int startRow = 0;
            int totalResults = -1;
            List<PlaceOld> places = await _db.Table<PlaceOld>();

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
                        foreach (var geoName in placesResponse.Geonames)
                        {
                            if (places.Find(x => x.Name == geoName.Name) == null)
                            {
                                var place = new PlaceOld
                                {
                                    Name = geoName.Name,
                                    Province = geoName.Province,
                                    Lat = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                                    Lon = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                                    Population = geoName.Population
                                };
                                await _db.InsertAsync(place);
                                places.Add(place);
                            }
                        }
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
                            foreach (var geoName in placesResponse.Geonames)
                            {
                                if (places.Find(x => x.Name == geoName.Name) == null)
                                {
                                    var place = new PlaceOld
                                    {
                                        Name = geoName.Name,
                                        Province = geoName.Province,
                                        Lat = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                                        Lon = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                                        Population = geoName.Population
                                    };
                                    await _db.InsertAsync(place);
                                    places.Add(place);
                                }
                            }
                            startRow += maxRows;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        return null;
                    }
                }
            }

            return await _db.Table<PlaceOld>();
        }

        public async Task<List<PlaceOld>> GetPlacesByProvince(string province)
        {
            return await _db.QueryAsync<PlaceOld>($"SELECT * FROM PlaceOld WHERE Name LIKE '%{province}%'");
        }

        public async Task<List<PlaceOld>> GetPlacesByType(string type)
        {
            return await _db.QueryAsync<PlaceOld>($"SELECT * FROM PlaceOld WHERE Name LIKE '%{type}%'");
        }

        public async Task<PlaceOld> GetPlaceByName(string name)
        {
            return await _db.FindWithQueryAsync<PlaceOld>($"SELECT * FROM PlaceOld WHERE Name = '{name}'");
        }

        public async Task<PlaceOld> GetPlaceById(int id)
        {
            return await _db.FindWithQueryAsync<PlaceOld>($"SELECT * FROM PlaceOld WHERE Id = {id}");
        }

        public async Task<List<PlaceOld>> GetAllNetherlandsPlaces()
        {
            return await _db.Table<PlaceOld>();
        }

        public async Task<List<Place>> AddPlacesToMariaDb(List<PlaceOld> placeOlds)
        {
            HashSet<string> alreadyProcessed = new(StringComparer.OrdinalIgnoreCase);
            List<Place> placesToAdd = new();

            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            foreach (PlaceOld placeOld in placeOlds)
            {
                var normalizedName = placeOld.Name.Trim();

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
                    Place place = new()
                    {
                        Name = normalizedName,
                        Population = placeOld.Population,
                        Province = placeOld.Province.Trim(),
                        Latitude = placeOld.Lat,
                        Longitude = placeOld.Lon
                    };

                    placesToAdd.Add(place);

                    Console.WriteLine($"Adding place: {normalizedName}");
                }
            }

            if (placesToAdd.Any())
            {
                await _dbContext.Places.AddRangeAsync(placesToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine("All changes saved.");
            }

            return await _dbContext.Places.ToListAsync();
        }
    }
}