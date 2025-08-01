using MolenApplicatie.Server.Models;
using System.Text.Json;
using System.Globalization;
using MolenApplicatie.Server.Utils;

namespace MolenApplicatie.Server.Services
{
    public class PlacesService
    {
        readonly string PathAlleInformatieMolens = $"Json/AlleInformatieMolens.json";
        private readonly HttpClient _client;
        private List<string> allowedTypes = new List<string>();

        private readonly DbConnection _db;

        public PlacesService()
        {
            _client = new HttpClient();
            _db = new DbConnection(Globals.DBAlleMolens);
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
            List<Place> places = await _db.Table<Place>();

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
                            if (places.Find(x=> x.Name == geoName.Name) == null)
                            { 
                                var place = new Place
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
                                    var place = new Place
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

            return await _db.Table<Place>();
        }

        public async Task<List<Place>> GetPlacesByProvince(string province)
        {
            return await _db.QueryAsync<Place>($"SELECT * FROM Place WHERE Name LIKE '%{province}%'");
        }

        public async Task<List<Place>> GetPlacesByType(string type)
        {
            return await _db.QueryAsync<Place>($"SELECT * FROM Place WHERE Name LIKE '%{type}%'");
        }

        public async Task<Place> GetPlaceByName(string name)
        {
            return await _db.FindWithQueryAsync<Place>($"SELECT * FROM Place WHERE Name = '{name}'");
        }

        public async Task<Place> GetPlaceById(int id)
        {
            return await _db.FindWithQueryAsync<Place>($"SELECT * FROM Place WHERE Id = {id}");
        }

        public async Task<List<Place>> GetAllNetherlandsPlaces()
        {
            return await _db.Table<Place>();
        }
    }
}