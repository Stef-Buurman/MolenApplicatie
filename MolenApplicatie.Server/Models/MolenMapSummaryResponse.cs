using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Models
{
    public sealed class MolenMapSummaryResponse
    {
        public int TotalMolensWithImage { get; init; }
        public List<RecentAddedImages> RecentAddedImages { get; init; } = [];
    }
}
