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
        [JsonPropertyName("adminCode1")]
        public string AdminCode1 { get; set; }

        [JsonPropertyName("lng")]
        public string Longitude { get; set; }

        [JsonPropertyName("geonameId")]
        public int GeonameId { get; set; }

        [JsonPropertyName("toponymName")]
        public string ToponymName { get; set; }

        [JsonPropertyName("countryId")]
        public string CountryId { get; set; }

        [JsonPropertyName("fcl")]
        public string Fcl { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fclName")]
        public string FclName { get; set; }

        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        [JsonPropertyName("fcodeName")]
        public string FcodeName { get; set; }

        [JsonPropertyName("adminName1")]
        public string Province { get; set; }

        [JsonPropertyName("lat")]
        public string Latitude { get; set; }

        [JsonPropertyName("fcode")]
        public string Fcode { get; set; }
    }
}
