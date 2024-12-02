using System.Text.Json.Serialization;

namespace MolenApplicatie.Server.Models
{
    public class GeoNamesResponse
    {
        [JsonPropertyName("totalResultsCount")]
        public int TotalResultsCount { get; set; }

        [JsonPropertyName("geonames")]
        public List<GeoName> Geonames { get; set; }
    }

    public class GeoName
    {
        [JsonPropertyName("geonameId")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("adminName1")]
        public string Province { get; set; }

        [JsonPropertyName("lat")]
        public string Latitude { get; set; }

        [JsonPropertyName("lng")]
        public string Longitude { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }
    }

}
