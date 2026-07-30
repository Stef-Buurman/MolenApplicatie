using System.Text.Json.Serialization;

namespace MolenApplicatie.Server.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(MapPointResponse), "point")]
    [JsonDerivedType(typeof(MapClusterResponse), "cluster")]
    public abstract class MapItemResponse
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    public sealed class MapPointResponse : MapItemResponse
    {
        public required string Url { get; init; }
        public string? PopupText { get; init; }
        public string? Toestand { get; init; }
        public List<string> Types { get; init; } = [];
        public bool HasImage { get; init; }
    }

    public sealed class MapClusterResponse : MapItemResponse
    {
        public required string ClusterId { get; init; }
        public int PointCount { get; init; }
        public int ExpansionZoom { get; init; }
        public PopupData? PopupData { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<MapCoordinateResponse>? Outline { get; init; }
    }

    public sealed class MapCoordinateResponse
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    public sealed class PopupData
    {
        public required string Title { get; init; }
        public List<PointData> PointData { get; init; } = [];
    }

    public sealed class PointData
    {
        public required string Url { get; init; }
        public string? PopupText { get; init; }
    }
}
