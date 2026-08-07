using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services;

public static class MapQueryBuilder
{
    public static IQueryable<MapQueryPoint> CreateGridQuery<TEntity>(
        IQueryable<TEntity> query,
        int zoom,
        Expression<Func<TEntity, Guid>> idSelector,
        Expression<Func<TEntity, double?>> latitudeSelector,
        Expression<Func<TEntity, double?>> longitudeSelector,
        Expression<Func<TEntity, double?>> mercatorYSelector)
    {
        var projection = CreateGridSourceProjection(idSelector, latitudeSelector, longitudeSelector, mercatorYSelector);
        var gridSizeInDegrees = GetGridSizeInDegrees(zoom);
        var validQuery = query.Select(projection).Where(point => point.Latitude != null && point.Longitude != null && point.MercatorY != null);

        return validQuery
            .GroupBy(point => new
            {
                CellX = (long)Math.Floor((point.Longitude!.Value + 180d) / gridSizeInDegrees),
                CellY = (long)Math.Floor(point.MercatorY!.Value / gridSizeInDegrees)
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
        var coordinatePredicate = CreateCoordinatePredicate(latitudeSelector, longitudeSelector);

        var projection = CreateIndividualProjection(
            idSelector,
            friendlyViewSelector,
            latitudeSelector,
            longitudeSelector,
            toestandSelector,
            typesSelector,
            hasImageSelector);

        return query.Where(coordinatePredicate).Select(projection);
    }

    private static double GetGridSizeInDegrees(int zoom)
    {
        return zoom switch
        {
            <= 3 => 16d,
            <= 4 => 10d,
            <= 5 => 6d,
            <= 6 => 2.75d,
            <= 7 => 1d,
            <= 8 => 0.55d,
            <= 9 => 0.3d,
            <= 10 => 0.14d,
            <= 11 => 0.08d,
            <= 12 => 0.04d,
            <= 13 => 0.02d,
            <= 14 => 0.01d,
            <= 15 => 0.005d,
            <= 16 => 0.0025d,
            <= 17 => 0.00125d,
            <= 18 => 0.000625d,
            <= 19 => 0.0003125d,
            _ => throw new ArgumentOutOfRangeException(nameof(zoom), zoom, "No grid size is configured for this zoom level.")
        };
    }

    private static Expression<Func<TEntity, bool>> CreateCoordinatePredicate<TEntity>(Expression<Func<TEntity, double?>> latitudeSelector, Expression<Func<TEntity, double?>> longitudeSelector)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var latitude = ReplaceParameter(latitudeSelector, parameter);
        var longitude = ReplaceParameter(longitudeSelector, parameter);

        var latitudeValue = Expression.Property(latitude, nameof(Nullable<double>.Value));
        var longitudeValue = Expression.Property(longitude, nameof(Nullable<double>.Value));

        var body = Expression.AndAlso(
            Expression.AndAlso(
                Expression.NotEqual(
                    latitude,
                    Expression.Constant(null, typeof(double?))),
                Expression.NotEqual(
                    longitude,
                    Expression.Constant(null, typeof(double?)))),
            Expression.AndAlso(
                Expression.AndAlso(
                    Expression.GreaterThanOrEqual(
                        latitudeValue,
                        Expression.Constant(-90d)),
                    Expression.LessThanOrEqual(
                        latitudeValue,
                        Expression.Constant(90d))),
                Expression.AndAlso(
                    Expression.GreaterThanOrEqual(
                        longitudeValue,
                        Expression.Constant(-180d)),
                    Expression.LessThanOrEqual(
                        longitudeValue,
                        Expression.Constant(180d)))));

        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression<Func<TEntity, MapQueryPoint>> CreateIndividualProjection<TEntity>(
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

        var typesList = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), [typeof(string)], types);

        var projection = Expression.MemberInit(
            Expression.New(typeof(MapQueryPoint)),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.Id))!,
                id),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.FriendlyView))!,
                friendlyView),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.Latitude))!,
                Expression.Property(latitude, nameof(Nullable<double>.Value))),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.Longitude))!,
                Expression.Property(longitude, nameof(Nullable<double>.Value))),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.PointCount))!,
                Expression.Constant(1)),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.Toestand))!,
                toestand),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.Types))!,
                typesList),
            Expression.Bind(
                typeof(MapQueryPoint).GetProperty(nameof(MapQueryPoint.HasImage))!,
                hasImage));

        return Expression.Lambda<Func<TEntity, MapQueryPoint>>(projection, parameter);
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

        return Expression.Lambda<Func<TEntity, MapSourcePoint>>(projection, parameter);
    }

    private static Expression ReplaceParameter<TEntity, TValue>(Expression<Func<TEntity, TValue>> expression, ParameterExpression replacementParameter)
        => new ParameterReplaceVisitor(expression.Parameters[0], replacementParameter).Visit(expression.Body)!;

    private sealed class MapSourcePoint
    {
        public Guid Id { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public double? MercatorY { get; init; }
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
        => node == _source ? _replacement : base.VisitParameter(node);
    }
}
