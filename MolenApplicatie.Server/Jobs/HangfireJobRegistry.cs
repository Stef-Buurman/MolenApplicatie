using Hangfire;

namespace MolenApplicatie.Server.Jobs
{
    public static class HangfireJobRegistry
    {
        private const string MillDatabaseImportRecurringJobId =
            "import-milldatabase-weekly";

        public static void Register()
        {
            RecurringJob.AddOrUpdate<MillDatabaseImportJob>(
                MillDatabaseImportRecurringJobId,
                job => job.ImportAsync(null!, CancellationToken.None),
                Cron.Weekly(DayOfWeek.Monday, 2),
                new RecurringJobOptions
                {
                    TimeZone = GetAmsterdamTimeZone()
                });
        }

        private static TimeZoneInfo GetAmsterdamTimeZone()
        {
            string[] timeZoneIds =
            [
                "Europe/Amsterdam",
                "W. Europe Standard Time"
            ];

            foreach (var timeZoneId in timeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {

                }
                catch (InvalidTimeZoneException)
                {

                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
