using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Records;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services
{
    public class MapClusterService
    {
        private const int MaximumZoom = 19;
        private const int UseDetailedQueryFromZoom = 8;
        private const int MaximumVisibleLocationsForExactPointDisplay = 20;
        private const int MaximumVisiblePointsForDetailedQuery = 200;
        private const double FullWorldLongitudeWidth = 360d;
        private const int VerySparseVisiblePointThreshold = 100;
        private const int SparseVisiblePointThreshold = 200;
        private const int MinimumClusterRadiusInPixels = 45;
        private const double VerySparseClusterRadiusMultiplier = 0.35d;
        private const double SparseClusterRadiusMultiplier = 0.5d;

        public async Task<IReadOnlyList<MapItemResponse>> GetMapItemsAsync<TEntity>(
            IQueryable<TEntity> query,
            Func<double, double, double, double, Expression<Func<TEntity, bool>>> viewportPredicateFactory,
            Func<IQueryable<TEntity>, IQueryable<Guid>> idQueryFactory,
            Func<IQueryable<TEntity>, IQueryable<MapCoordinateQueryPoint>> coordinateQueryFactory,
            Func<IQueryable<TEntity>, IQueryable<MapQueryPoint>> gridQueryFactory,
            Func<IQueryable<TEntity>, IQueryable<MapQueryPoint>> individualMapQueryFactory,
            Func<IQueryable<TEntity>, IReadOnlyCollection<Guid>, IQueryable<MapQueryPoint>> individualPointQueryFactory,
            MapViewport viewport,
            Func<MapQueryPoint, string> urlFactory,
            Func<MapQueryPoint, string> popupTextFactory,
            Func<IReadOnlyCollection<MapQueryPoint>, string> popupTitleFactory,
            CancellationToken token = default)
            where TEntity : class
        {
            ValidateViewport(viewport);

            var bounds = NormalizeBounds(viewport.West, viewport.East);
            var viewportQuery = CreateViewportQuery(
                query,
                viewportPredicateFactory,
                bounds.West,
                viewport.South,
                bounds.East,
                viewport.North);

            var showEveryVisibleLocation = await HasAtMostVisibleLocationsAsync(
                coordinateQueryFactory(viewportQuery),
                MaximumVisibleLocationsForExactPointDisplay,
                token);

            if (showEveryVisibleLocation)
            {
                var exactDatabasePoints = await individualMapQueryFactory(viewportQuery)
                    .ToListAsync(token);

                token.ThrowIfCancellationRequested();

                var exactVisiblePoints = CreateVisiblePoints(
                    exactDatabasePoints,
                    bounds.West,
                    bounds.East);

                return CreatePointOrExactLocationResponses(
                    exactVisiblePoints,
                    viewport.Zoom,
                    urlFactory,
                    popupTextFactory,
                    popupTitleFactory,
                    token);
            }

            var useDetailedQuery =
                viewport.Zoom >= UseDetailedQueryFromZoom &&
                await HasAtMostVisiblePointsAsync(
                    idQueryFactory(viewportQuery),
                    MaximumVisiblePointsForDetailedQuery,
                    token);

            var databasePoints = await (
                    useDetailedQuery
                        ? individualMapQueryFactory(viewportQuery)
                        : gridQueryFactory(viewportQuery))
                .ToListAsync(token);

            token.ThrowIfCancellationRequested();

            var visiblePoints = CreateVisiblePoints(databasePoints, bounds.West, bounds.East);

            if (visiblePoints.Any(point => point.Point.IsGridCell))
            {
                var singlePointIds = visiblePoints
                    .Where(point => point.Point.PointCount == 1)
                    .Select(point => point.Point.Id)
                    .Distinct()
                    .ToArray();

                var individualPoints = singlePointIds.Length == 0
                    ? []
                    : await individualPointQueryFactory(
                            viewportQuery,
                            singlePointIds)
                        .ToListAsync(token);

                var visibleIndividualPoints = CreateVisiblePoints(
                        individualPoints,
                        bounds.West,
                        bounds.East)
                    .ToDictionary(point => point.Point.Id);

                return CreateDatabaseGridResponses(
                    visiblePoints,
                    visibleIndividualPoints,
                    viewport.Zoom,
                    urlFactory,
                    popupTextFactory);
            }

            return CreateClusters(
                visiblePoints,
                viewport.Zoom,
                urlFactory,
                popupTextFactory,
                popupTitleFactory,
                token);
        }

        private static IReadOnlyList<MapItemResponse> CreateDatabaseGridResponses(
            IReadOnlyCollection<VisibleMapPoint> points,
            IReadOnlyDictionary<Guid, VisibleMapPoint> individualPoints,
            int zoom,
            Func<MapQueryPoint, string> urlFactory,
            Func<MapQueryPoint, string> popupTextFactory)
        {
            var results = new List<MapItemResponse>(points.Count);

            foreach (var point in points)
            {
                if (
                    point.Point.PointCount == 1 &&
                    individualPoints.TryGetValue(point.Point.Id, out var individualPoint))
                {
                    results.Add(CreatePointResponse(
                        individualPoint,
                        urlFactory,
                        popupTextFactory));

                    continue;
                }

                results.Add(new MapClusterResponse
                {
                    ClusterId = CreateClusterId(point.Point, zoom),
                    Latitude = point.Point.Latitude,
                    Longitude = point.DisplayLongitude,
                    PointCount = point.Point.PointCount,
                    ExpansionZoom = Math.Min(zoom + 2, MaximumZoom)
                });
            }

            return results;
        }

        private static string CreateClusterId(MapQueryPoint point, int zoom)
        {
            if (point.CellX.HasValue && point.CellY.HasValue)
                return $"{zoom}:{point.CellX.Value}:{point.CellY.Value}";

            return $"{zoom}:{point.Id}";
        }

        private static IReadOnlyList<MapItemResponse> CreatePointOrExactLocationResponses(
            IReadOnlyCollection<VisibleMapPoint> points,
            int zoom,
            Func<MapQueryPoint, string> urlFactory,
            Func<MapQueryPoint, string> popupTextFactory,
            Func<IReadOnlyCollection<MapQueryPoint>, string> popupTitleFactory,
            CancellationToken token)
        {
            var coordinateGroups = new Dictionary<CoordinateKey, List<VisibleMapPoint>>();

            foreach (var point in points)
            {
                token.ThrowIfCancellationRequested();

                var key = new CoordinateKey(
                    point.Point.Latitude,
                    point.DisplayLongitude);

                if (!coordinateGroups.TryGetValue(key, out var coordinatePoints))
                {
                    coordinatePoints = [];
                    coordinateGroups.Add(key, coordinatePoints);
                }

                coordinatePoints.Add(point);
            }

            var results = new List<MapItemResponse>(coordinateGroups.Count);

            foreach (var coordinatePoints in coordinateGroups.Values)
            {
                token.ThrowIfCancellationRequested();

                var firstPoint = coordinatePoints[0];

                if (coordinatePoints.Count == 1)
                {
                    results.Add(CreatePointResponse(
                        firstPoint,
                        urlFactory,
                        popupTextFactory));

                    continue;
                }

                var mapPoints = coordinatePoints
                    .Select(point => point.Point)
                    .ToList();

                results.Add(new MapClusterResponse
                {
                    ClusterId = CreateExactCoordinateClusterId(firstPoint.Point, zoom),
                    Latitude = firstPoint.Point.Latitude,
                    Longitude = firstPoint.DisplayLongitude,
                    PointCount = coordinatePoints.Count,
                    ExpansionZoom = Math.Min(zoom + 2, MaximumZoom),
                    PopupData = new PopupData
                    {
                        Title = popupTitleFactory(mapPoints),
                        PointData = coordinatePoints
                            .Select(point => new PointData
                            {
                                Url = urlFactory(point.Point),
                                PopupText = popupTextFactory(point.Point)
                            })
                            .ToList()
                    }
                });
            }

            return results;
        }

        private static string CreateExactCoordinateClusterId(MapQueryPoint point, int zoom)
        {
            return $"{zoom}:exact:{point.Latitude:R}:{point.Longitude:R}";
        }

        private readonly record struct CoordinateKey(
            double Latitude,
            double Longitude);

        private static int GetTotalPointCount(IEnumerable<VisibleMapPoint> points)
        {
            return points.Sum(point => point.Point.PointCount);
        }

        private static IQueryable<TEntity> CreateViewportQuery<TEntity>(
            IQueryable<TEntity> query,
            Func<double, double, double, double, Expression<Func<TEntity, bool>>> viewportPredicateFactory,
            double west,
            double south,
            double east,
            double north)
            where TEntity : class
        {
            var viewportWidth = east - west;
            var filteredQuery = query.AsNoTracking();

            if (viewportWidth >= FullWorldLongitudeWidth)
            {
                return filteredQuery.Where(
                    viewportPredicateFactory(
                        -180d,
                        south,
                        180d,
                        north));
            }

            var longitudeRanges = CreateDatabaseLongitudeRanges(west, east);

            if (longitudeRanges.Count == 1)
            {
                var range = longitudeRanges[0];

                return filteredQuery.Where(
                    viewportPredicateFactory(
                        range.West,
                        south,
                        range.East,
                        north));
            }

            var firstPredicate = viewportPredicateFactory(
                longitudeRanges[0].West,
                south,
                longitudeRanges[0].East,
                north);

            var secondPredicate = viewportPredicateFactory(
                longitudeRanges[1].West,
                south,
                longitudeRanges[1].East,
                north);

            // Keep this as one keyed entity query. Concat/Union followed by an
            // individual projection with the Types collection cannot reliably
            // be translated by EF Core/Pomelo around the date line.
            return filteredQuery.Where(
                CombineWithOrElse(
                    firstPredicate,
                    secondPredicate));
        }

        private static async Task<bool> HasAtMostVisibleLocationsAsync(
            IQueryable<MapCoordinateQueryPoint> coordinateQuery,
            int maximumLocationCount,
            CancellationToken token)
        {
            var cappedLocationCount = await coordinateQuery
                .Select(point => new
                {
                    point.Latitude,
                    point.Longitude
                })
                .Distinct()
                .OrderBy(point => point.Latitude)
                .ThenBy(point => point.Longitude)
                .Take(maximumLocationCount + 1)
                .CountAsync(token);

            return cappedLocationCount <= maximumLocationCount;
        }

        private static async Task<bool> HasAtMostVisiblePointsAsync(
            IQueryable<Guid> idQuery,
            int maximumPointCount,
            CancellationToken token)
        {
            var cappedPointCount = await idQuery
                .OrderBy(id => id)
                .Take(maximumPointCount + 1)
                .CountAsync(token);

            return cappedPointCount <= maximumPointCount;
        }

        private static Expression<Func<TEntity, bool>> CombineWithOrElse<TEntity>(
            Expression<Func<TEntity, bool>> first,
            Expression<Func<TEntity, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "entity");

            var firstBody = new ParameterReplaceVisitor(
                    first.Parameters[0],
                    parameter)
                .Visit(first.Body)!;

            var secondBody = new ParameterReplaceVisitor(
                    second.Parameters[0],
                    parameter)
                .Visit(second.Body)!;

            return Expression.Lambda<Func<TEntity, bool>>(
                Expression.OrElse(firstBody, secondBody),
                parameter);
        }

        private static IReadOnlyList<VisibleMapPoint> CreateVisiblePoints(
            IReadOnlyCollection<MapQueryPoint> points,
            double west,
            double east)
        {
            var viewportCenterLongitude = west + (east - west) / 2d;
            var visiblePoints = new List<VisibleMapPoint>(points.Count);

            foreach (var point in points)
            {
                var displayLongitude = GetClosestWorldLongitude(point.Longitude, viewportCenterLongitude);

                if (displayLongitude < west)
                    displayLongitude += FullWorldLongitudeWidth;
                else if (displayLongitude > east)
                    displayLongitude -= FullWorldLongitudeWidth;

                visiblePoints.Add(new VisibleMapPoint
                {
                    Point = point,
                    DisplayLongitude = displayLongitude
                });
            }

            return visiblePoints;
        }

        private static int GetClusterRadiusInPixels(int zoom, int visiblePointCount)
        {
            var normalRadius = zoom switch
            {
                <= 13 => 140,
                <= 14 => 125,
                <= 15 => 110,
                <= 16 => 95,
                _ => 80
            };

            if (visiblePointCount <= VerySparseVisiblePointThreshold)
            {
                return Math.Max(
                    MinimumClusterRadiusInPixels,
                    (int)Math.Round(normalRadius * VerySparseClusterRadiusMultiplier));
            }

            if (visiblePointCount <= SparseVisiblePointThreshold)
            {
                return Math.Max(
                    MinimumClusterRadiusInPixels,
                    (int)Math.Round(normalRadius * SparseClusterRadiusMultiplier));
            }

            return normalRadius;
        }

        private static IReadOnlyList<LongitudeRange> CreateDatabaseLongitudeRanges(double west, double east)
        {
            var viewportWidth = east - west;

            if (viewportWidth >= FullWorldLongitudeWidth)
                return [new LongitudeRange(-180d, 180d)];

            var normalizedWest = NormalizeLongitude(west);
            var normalizedEast = normalizedWest + viewportWidth;

            if (normalizedEast <= 180d)
                return [new LongitudeRange(normalizedWest, normalizedEast)];

            return
            [
                new LongitudeRange(normalizedWest, 180d),
                new LongitudeRange(-180d, normalizedEast - FullWorldLongitudeWidth)
            ];
        }

        private static IReadOnlyList<MapItemResponse> CreateClusters(
            IReadOnlyCollection<VisibleMapPoint> points,
            int zoom,
            Func<MapQueryPoint, string> urlFactory,
            Func<MapQueryPoint, string> popupTextFactory,
            Func<IReadOnlyCollection<MapQueryPoint>, string> popupTitleFactory,
            CancellationToken token)
        {
            var visiblePointCount = GetTotalPointCount(points);
            var clusterRadiusInPixels = GetClusterRadiusInPixels(zoom, visiblePointCount);
            var spatialGrid = new Dictionary<GridCellKey, List<ClusterAccumulator>>();

            foreach (var point in points)
            {
                token.ThrowIfCancellationRequested();

                var pixelPosition = ConvertToWorldPixel(
                    point.Point.Latitude,
                    point.DisplayLongitude,
                    zoom);

                var gridX = (long)Math.Floor(pixelPosition.X / clusterRadiusInPixels);
                var gridY = (long)Math.Floor(pixelPosition.Y / clusterRadiusInPixels);

                var closestCluster = FindClosestCluster(
                    spatialGrid,
                    gridX,
                    gridY,
                    pixelPosition,
                    clusterRadiusInPixels);

                if (closestCluster is not null)
                {
                    closestCluster.Add(point, pixelPosition);
                    continue;
                }

                var gridCellKey = new GridCellKey(gridX, gridY);

                if (!spatialGrid.TryGetValue(gridCellKey, out var clustersInCell))
                {
                    clustersInCell = [];
                    spatialGrid.Add(gridCellKey, clustersInCell);
                }

                clustersInCell.Add(new ClusterAccumulator(point, pixelPosition));
            }

            var results = new List<MapItemResponse>();

            foreach (var clustersInCell in spatialGrid.Values)
            {
                token.ThrowIfCancellationRequested();

                foreach (var cluster in clustersInCell)
                {
                    if (cluster.Count == 1)
                    {
                        results.Add(CreatePointResponse(
                            cluster.FirstPoint,
                            urlFactory,
                            popupTextFactory));

                        continue;
                    }

                    var popupData = cluster.AllPointsHaveSameCoordinates
                        ? new PopupData
                        {
                            Title = popupTitleFactory(
                                cluster.Points
                                    .Select(point => point.Point)
                                    .ToList()),
                            PointData = cluster.Points
                                .Select(point => new PointData
                                {
                                    Url = urlFactory(point.Point),
                                    PopupText = popupTextFactory(point.Point)
                                })
                                .ToList()
                        }
                        : null;

                    results.Add(new MapClusterResponse
                    {
                        ClusterId = CreateClusterId(cluster.FirstPoint.Point, zoom),
                        Latitude = cluster.Latitude,
                        Longitude = cluster.Longitude,
                        PointCount = cluster.Count,
                        ExpansionZoom = Math.Min(zoom + 2, MaximumZoom),
                        PopupData = popupData,
                        Outline = CreateClusterOutline(cluster.Points)
                    });
                }
            }

            return results;
        }

        private static List<MapCoordinateResponse> CreateClusterOutline(IReadOnlyCollection<VisibleMapPoint> points)
        {
            var coordinates = points
                .Select(point => new Coordinate(
                    point.DisplayLongitude,
                    point.Point.Latitude))
                .DistinctBy(coordinate => (coordinate.X, coordinate.Y))
                .ToArray();

            if (coordinates.Length < 3)
                return [];

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var multiPoint = geometryFactory.CreateMultiPointFromCoords(coordinates);
            var convexHull = multiPoint.ConvexHull();

            if (convexHull is not Polygon polygon || polygon.IsEmpty)
                return [];

            var outlineCoordinates = polygon.ExteriorRing.Coordinates;

            return outlineCoordinates
                .Take(outlineCoordinates.Length - 1)
                .Select(coordinate => new MapCoordinateResponse
                {
                    Latitude = coordinate.Y,
                    Longitude = coordinate.X
                })
                .ToList();
        }

        private static ClusterAccumulator? FindClosestCluster(
            IReadOnlyDictionary<GridCellKey, List<ClusterAccumulator>> spatialGrid,
            long gridX,
            long gridY,
            WorldPixel pixelPosition,
            double maximumDistance)
        {
            ClusterAccumulator? closestCluster = null;
            var closestDistanceSquared = maximumDistance * maximumDistance;

            for (var xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (var yOffset = -1; yOffset <= 1; yOffset++)
                {
                    var neighbourKey = new GridCellKey(gridX + xOffset, gridY + yOffset);

                    if (!spatialGrid.TryGetValue(neighbourKey, out var neighbourClusters))
                        continue;

                    foreach (var cluster in neighbourClusters)
                    {
                        var xDistance = cluster.PixelX - pixelPosition.X;
                        var yDistance = cluster.PixelY - pixelPosition.Y;
                        var distanceSquared = xDistance * xDistance + yDistance * yDistance;

                        if (distanceSquared > closestDistanceSquared) continue;

                        closestDistanceSquared = distanceSquared;
                        closestCluster = cluster;
                    }
                }
            }

            return closestCluster;
        }

        private static MapPointResponse CreatePointResponse(
            VisibleMapPoint point,
            Func<MapQueryPoint, string> urlFactory,
            Func<MapQueryPoint, string> popupTextFactory)
        {
            return new MapPointResponse
            {
                Latitude = point.Point.Latitude,
                Longitude = point.DisplayLongitude,
                Url = urlFactory(point.Point),
                PopupText = popupTextFactory(point.Point),
                Toestand = point.Point.Toestand,
                Types = point.Point.Types,
                HasImage = point.Point.HasImage
            };
        }

        private static NormalizedLongitudeBounds NormalizeBounds(double west, double east)
        {
            while (east <= west)
                east += FullWorldLongitudeWidth;

            return new NormalizedLongitudeBounds(west, east);
        }

        private static double NormalizeLongitude(double longitude)
        {
            var normalizedLongitude = longitude % FullWorldLongitudeWidth;

            if (normalizedLongitude < -180d)
                normalizedLongitude += FullWorldLongitudeWidth;
            else if (normalizedLongitude >= 180d)
                normalizedLongitude -= FullWorldLongitudeWidth;

            return normalizedLongitude;
        }

        private static double GetClosestWorldLongitude(double longitude, double targetLongitude)
        {
            var worldOffset = Math.Round((targetLongitude - longitude) / FullWorldLongitudeWidth);
            return longitude + worldOffset * FullWorldLongitudeWidth;
        }

        private static WorldPixel ConvertToWorldPixel(double latitude, double longitude, int zoom)
        {
            var constrainedLatitude = Math.Clamp(latitude, -85.05112878, 85.05112878);
            var latitudeRadians = constrainedLatitude * Math.PI / 180d;
            var mapSize = 256d * Math.Pow(2, zoom);

            var x = (longitude + 180d) / FullWorldLongitudeWidth * mapSize;
            var y = (1d - Math.Log(Math.Tan(latitudeRadians) + 1d / Math.Cos(latitudeRadians)) / Math.PI) / 2d * mapSize;

            return new WorldPixel(x, y);
        }

        private static void ValidateViewport(MapViewport viewport)
        {
            if (!double.IsFinite(viewport.West))
                throw new ArgumentException("West must be a finite number.", nameof(viewport));

            if (!double.IsFinite(viewport.East))
                throw new ArgumentException("East must be a finite number.", nameof(viewport));

            if (!double.IsFinite(viewport.South) || viewport.South < -90 || viewport.South > 90)
                throw new ArgumentException("South must be between -90 and 90.", nameof(viewport));

            if (!double.IsFinite(viewport.North) || viewport.North < -90 || viewport.North > 90)
                throw new ArgumentException("North must be between -90 and 90.", nameof(viewport));

            if (viewport.South >= viewport.North)
                throw new ArgumentException("South must be smaller than north.", nameof(viewport));

            if (viewport.Zoom < 0 || viewport.Zoom > MaximumZoom)
                throw new ArgumentException($"Zoom must be between 0 and {MaximumZoom}.", nameof(viewport));
        }


        private sealed class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly ParameterExpression _replacement;

            public ParameterReplaceVisitor(
                ParameterExpression source,
                ParameterExpression replacement)
            {
                _source = source;
                _replacement = replacement;
            }

            protected override Expression VisitParameter(
                ParameterExpression node)
            {
                return node == _source
                    ? _replacement
                    : base.VisitParameter(node);
            }
        }

        private sealed class VisibleMapPoint
        {
            public required MapQueryPoint Point { get; init; }
            public double DisplayLongitude { get; init; }
        }

        private sealed class ClusterAccumulator
        {
            private const double CoordinateTolerance = 0.000000001d;

            private double _latitudeTotal;
            private double _longitudeTotal;
            private double _pixelXTotal;
            private double _pixelYTotal;

            public ClusterAccumulator(
                VisibleMapPoint firstPoint,
                WorldPixel pixelPosition)
            {
                FirstPoint = firstPoint;
                Add(firstPoint, pixelPosition);
            }

            public int Count { get; private set; }
            public VisibleMapPoint FirstPoint { get; }
            public List<VisibleMapPoint> Points { get; } = [];
            public double Latitude => _latitudeTotal / Count;
            public double Longitude => _longitudeTotal / Count;
            public double PixelX => _pixelXTotal / Count;
            public double PixelY => _pixelYTotal / Count;

            public bool AllPointsHaveSameCoordinates =>
                Points.All(point =>
                    Math.Abs(point.Point.Latitude - FirstPoint.Point.Latitude) <= CoordinateTolerance &&
                    Math.Abs(point.DisplayLongitude - FirstPoint.DisplayLongitude) <= CoordinateTolerance);

            public void Add(
                VisibleMapPoint point,
                WorldPixel pixelPosition)
            {
                var pointCount = point.Point.PointCount;

                Count += pointCount;
                _latitudeTotal += point.Point.Latitude * pointCount;
                _longitudeTotal += point.DisplayLongitude * pointCount;
                _pixelXTotal += pixelPosition.X * pointCount;
                _pixelYTotal += pixelPosition.Y * pointCount;

                Points.Add(point);
            }
        }

        private readonly record struct GridCellKey(long X, long Y);
        private readonly record struct WorldPixel(double X, double Y);
        private readonly record struct LongitudeRange(double West, double East);
        private readonly record struct NormalizedLongitudeBounds(double West, double East);
    }
}