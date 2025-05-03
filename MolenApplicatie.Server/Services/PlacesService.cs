using MolenApplicatie.Server.Models;
using System.Text.Json;
using System.Globalization;
using MolenApplicatie.Server.Utils;
using MolenApplicatie.Server.Models.MariaDB;
using MolenApplicatie.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MolenApplicatie.Server.Services
{
    public class PlacesService
    {
        private readonly HttpClient _client;
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

        public async Task<List<PlaceOld>> ReadAllNetherlandsPlaces2_0()
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

                        //foreach (var geoName in placesResponse.Geonames)
                        //{

                        //if (places.Find(x => x.Name == geoName.Name) == null)
                        //{
                        //    var place = new PlaceOld
                        //    {
                        //        Name = geoName.Name,
                        //        Province = geoName.Province,
                        //        Lat = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                        //        Lon = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                        //        Population = geoName.Population
                        //    };
                        //    await _db.InsertAsync(place);
                        //    places.Add(place);
                        //}
                        //}
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
                            //foreach (var geoName in placesResponse.Geonames)
                            //{
                            //    if (places.Find(x => x.Name == geoName.Name) == null)
                            //    {
                            //        var place = new PlaceOld
                            //        {
                            //            Name = geoName.Name,
                            //            Province = geoName.Province,
                            //            Lat = double.Parse(geoName.Latitude, CultureInfo.InvariantCulture),
                            //            Lon = double.Parse(geoName.Longitude, CultureInfo.InvariantCulture),
                            //            Population = geoName.Population
                            //        };
                            //        await _db.InsertAsync(place);
                            //        places.Add(place);
                            //    }
                            //}
                            //startRow += maxRows;
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

        public async Task<List<Place>> AddPlacesToMariaDb(List<Place> placeOlds)
        {
            HashSet<string> alreadyProcessed = new(StringComparer.OrdinalIgnoreCase);
            List<Place> placesToAdd = new();

            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            foreach (Place place in placeOlds)
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

        public async Task<List<Place>> AddPlacesToMariaDb(List<PlaceOld> placeOlds)
        {
            List<Place> place = placeOlds.Select(placeOld => new Place()
            {
                Name = placeOld.Name.Trim(),
                Population = placeOld.Population,
                Province = placeOld.Province.Trim(),
                Latitude = placeOld.Lat,
                Longitude = placeOld.Lon
            }).ToList();

            return await AddPlacesToMariaDb(place);
        }
    }
}