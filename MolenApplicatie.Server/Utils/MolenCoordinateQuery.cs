using System.Linq.Expressions;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Utils
{
    public static class MolenCoordinateQuery
    {
        public static Expression<Func<MolenData, bool>> HasUsableCoordinates => molen =>
            molen.Latitude >= -90d &&
            molen.Latitude <= 90d &&
            molen.Longitude >= -180d &&
            molen.Longitude <= 180d &&
            !(molen.Latitude == 0d && molen.Longitude == 0d);
    }
}
