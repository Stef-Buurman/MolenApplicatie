using Hangfire;
using Hangfire.Server;
using MolenApplicatie.Server.Models;
using MolenApplicatie.Server.Services;

namespace MolenApplicatie.Server.Jobs
{
    public sealed class MillDatabaseImportJob
    {
        private readonly MillDatabaseRemoteImportService _importService;
        private readonly ILogger<MillDatabaseImportJob> _logger;

        public MillDatabaseImportJob(MillDatabaseRemoteImportService importService, ILogger<MillDatabaseImportJob> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [Queue("default")]
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 21_600)]
        public async Task ImportAsync(PerformContext hangfireContext, CancellationToken cancellationToken)
        {
            var progressService = new ProgressService(ProgressState.MillDatabaseImportProgress, hangfireContext);

            progressService
                .Reset()
                .SetStepNumber(1)
                .SetIsActive(true)
                .SetProgressMessage(
                    "Starting the scheduled Mill Database CSV import...")
                .Commit();

            _logger.LogInformation(
                "Starting the scheduled Mill Database CSV import.");

            try
            {
                var result = await _importService.ImportAsync(progressService, cancellationToken);
                WriteResultToConsole(progressService, result);

                _logger.LogInformation(
                    "Mill Database CSV import completed. " +
                    "Total rows: {TotalRows}, imported rows: {ImportedRows}, " +
                    "added mills: {AddedMolens}, updated mills: {UpdatedMolens}, " +
                    "skipped existing mills: {SkippedExistingMolens}, " +
                    "skipped invalid rows: {SkippedInvalidRows}.",
                    result.TotalRows,
                    result.ImportedRows,
                    result.AddedMolens,
                    result.UpdatedMolens,
                    result.SkippedExistingMolens,
                    result.SkippedInvalidRows);

                if (result.SkippedExistingMolens > 0 || result.SkippedInvalidRows > 0)
                {
                    if (result.SkippedInvalidRows > 0)
                    {
                        _logger.LogWarning(
                            "Mill Database CSV import skipped {SkippedExistingMolens} " +
                            "existing/duplicate rows and {SkippedInvalidRows} invalid rows.",
                            result.SkippedExistingMolens,
                            result.SkippedInvalidRows);
                    }

                    progressService.SetProgressMessage($"Skipped: {result.SkippedExistingMolens:N0} existing/duplicate | " + $"{result.SkippedInvalidRows:N0} invalid.");
                }

                progressService.SetIsDoneSuccessfully("Mill Database CSV import completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("The scheduled Mill Database CSV import was cancelled.");

                progressService.SetIsCancelled("Mill Database CSV import was cancelled.");

                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "The scheduled Mill Database CSV import failed.");

                progressService.SetIsError().SetIsDoneWithErrors($"Mill Database CSV import failed: {exception.Message}");

                throw;
            }
        }

        private static void WriteResultToConsole(ProgressService progressService, MillDatabaseImportResult result)
        {
            progressService.SetProgressMessage(
                "Import result: " +
                $"{result.TotalRows:N0} rows read, " +
                $"{result.ImportedRows:N0} imported, " +
                $"{result.AddedMolens:N0} added, " +
                $"{result.UpdatedMolens:N0} updated, " +
                $"{result.SkippedExistingMolens:N0} skipped as existing, " +
                $"{result.SkippedInvalidRows:N0} invalid.");

            if (result.ToestandCounts.Count == 0) return;

            var states = string.Join(" | ", result.ToestandCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value:N0}"));

            progressService.SetProgressMessage($"Imported states: {states}");
        }
    }
}
