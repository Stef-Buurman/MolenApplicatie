using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services;

public static class MapQueryBuilder
{
    private const int UseExactCoordinatesFromZoom = 13;

    public static IQueryable<MapQueryPoint> Create<TEntity>(
        IQueryable<TEntity> query,
        int zoom,
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, string>> friendlyViewSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, double?>> mercatorYSelector,
        Expression<Func<TEntity, string?>> toestandSelector,
        Expression<Func<TEntity, IEnumerable<string>>> typesSelector,
        Expression<Func<TEntity, bool>> hasImageSelector)
    {
        if (zoom >= UseExactCoordinatesFromZoom)
        {
            return CreateIndividualPointQuery(
                query,
                idSelector,
                friendlyViewSelector,
                latitudeSelector,
                longitudeSelector,
                toestandSelector,
                typesSelector,
                hasImageSelector);
        }

        return CreateGridQuery(
            query,
            zoom,
            idSelector,
            latitudeSelector,
            longitudeSelector,
            mercatorYSelector);
    }

    public static IQueryable<MapQueryPoint> CreateIndividualPointQuery<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, string>> friendlyViewSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, string?>> toestandSelector,
        Expression<Func<TEntity, IEnumerable<string>>> typesSelector,
        Expression<Func<TEntity, bool>> hasImageSelector)
    {
        var projection = CreateIndividualSourceProjection(
            idSelector,
            friendlyViewSelector,
            latitudeSelector,
            longitudeSelector,
            toestandSelector,
            typesSelector,
            hasImageSelector);

        return query
            .Select(projection)
            .Where(point =>
                point.Latitude != null &&
                point.Longitude != null &&
                point.Latitude >= -90d &&
                point.Latitude <= 90d &&
                point.Longitude >= -180d &&
                point.Longitude <= 180d)
            .Select(point => new MapQueryPoint
            {
                Id = point.Id,
                FriendlyView = point.FriendlyView,
                Latitude = point.Latitude!.Value,
                Longitude = point.Longitude!.Value,
                PointCount = 1,
                Toestand = point.Toestand,
                Types = point.Types.ToList(),
                HasImage = point.HasImage
            });
    }

    private static IQueryable<MapQueryPoint> CreateGridQuery<TEntity>(
        IQueryable<TEntity> query,
        int zoom,
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, double?>> mercatorYSelector)
    {
        var projection = CreateGridSourceProjection(
            idSelector,
            latitudeSelector,
            longitudeSelector,
            mercatorYSelector);

        var gridSizeInDegrees = GetGridSizeInDegrees(zoom);

        var validQuery = query
            .Select(projection)
            .Where(point =>
                point.Latitude != null &&
                point.Longitude != null &&
                point.MercatorY != null);

        return validQuery
            .GroupBy(point => new
            {
                CellX = (long)Math.Floor(
                    (point.Longitude!.Value + 180d) /
                    gridSizeInDegrees),
                CellY = (long)Math.Floor(
                    point.MercatorY!.Value /
                    gridSizeInDegrees)
            })
            .Select(group => new MapQueryPoint
            {
                Id = group.Select(point => point.Id).First(),
                Latitude = group.Average(point => point.Latitude!.Value),
                Longitude = group.Average(point => point.Longitude!.Value),
                PointCount = group.Count(),
                CellX = group.Key.CellX,
                CellY = group.Key.CellY
            });
    }

    private static double GetGridSizeInDegrees(int zoom)
    {
        return zoom switch
        {
            <= 2 => 10d,
            <= 3 => 9d,
            <= 4 => 6d,
            <= 5 => 3.5d,
            <= 6 => 2d,
            <= 7 => 1d,
            <= 8 => 0.5d,
            <= 9 => 0.3d,
            <= 10 => 0.15d,
            <= 11 => 0.06d,
            <= 12 => 0.04d,
            _ => throw new ArgumentOutOfRangeException(
                nameof(zoom),
                zoom,
                "No grid size is configured for this zoom level.")
        };
    }

    private static Expression<Func<TEntity, MapSourcePoint>> CreateIndividualSourceProjection<TEntity>(
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, string>> friendlyViewSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, string?>> toestandSelector,
        Expression<Func<TEntity, IEnumerable<string>>> typesSelector,
        Expression<Func<TEntity, bool>> hasImageSelector)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");

        var id = ReplaceParameter(idSelector, parameter);
        var friendlyView = ReplaceParameter(friendlyViewSelector, parameter);
        var latitude = ReplaceParameter(latitudeSelector, parameter);
        var longitude = ReplaceParameter(longitudeSelector, parameter);
        var toestand = ReplaceParameter(toestandSelector, parameter);
        var types = ReplaceParameter(typesSelector, parameter);
        var hasImage = ReplaceParameter(hasImageSelector, parameter);

        var projection = Expression.MemberInit(
            Expression.New(typeof(MapSourcePoint)),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Id))!,
                id),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.FriendlyView))!,
                friendlyView),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Latitude))!,
                latitude),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Longitude))!,
                longitude),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Toestand))!,
                toestand),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Types))!,
                types),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.HasImage))!,
                hasImage));

        return Expression.Lambda<Func<TEntity, MapSourcePoint>>(
            projection,
            parameter);
    }

    private static Expression<Func<TEntity, MapSourcePoint>> CreateGridSourceProjection<TEntity>(
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, double?>> mercatorYSelector)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");

        var id = ReplaceParameter(idSelector, parameter);
        var latitude = ReplaceParameter(latitudeSelector, parameter);
        var longitude = ReplaceParameter(longitudeSelector, parameter);
        var mercatorY = ReplaceParameter(mercatorYSelector, parameter);

        var projection = Expression.MemberInit(
            Expression.New(typeof(MapSourcePoint)),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Id))!,
                id),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Latitude))!,
                latitude),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.Longitude))!,
                longitude),
            Expression.Bind(
                typeof(MapSourcePoint).GetProperty(nameof(MapSourcePoint.MercatorY))!,
                mercatorY));

        return Expression.Lambda<Func<TEntity, MapSourcePoint>>(
            projection,
            parameter);
    }

    private static Expression ReplaceParameter<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> expression,
        ParameterExpression replacementParameter)
    {
        return new ParameterReplaceVisitor(
                expression.Parameters[0],
                replacementParameter)
            .Visit(expression.Body)!;
    }

    private sealed class MapSourcePoint
    {
        public Guid Id { get; init; }
        public string FriendlyView { get; init; } = string.Empty;
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public double? MercatorY { get; init; }
        public string? Toestand { get; init; }
        public IEnumerable<string> Types { get; init; } = [];
        public bool HasImage { get; init; }
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

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _source
                ? _replacement
                : base.VisitParameter(node);
        }
    }
}
