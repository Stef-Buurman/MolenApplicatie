namespace MolenApplicatie.Server.Services
{
    public sealed class MapQueryPoint
    {
        public Guid Id { get; init; }
        public string FriendlyView { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public int PointCount { get; init; }
        public long? CellX { get; init; }
        public long? CellY { get; init; }
        public string? Toestand { get; init; }
        public List<string> Types { get; init; } = [];
        public bool HasImage { get; init; }

        public bool IsGridCell => CellX.HasValue && CellY.HasValue;
    }

    public sealed class MapCoordinateQueryPoint
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }
}
