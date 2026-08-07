using Hangfire;
using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Jobs;

namespace MolenApplicatie.Server.Services
{
    public sealed class DatabaseInitializationHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<DatabaseInitializationHostedService> _logger;

        public DatabaseInitializationHostedService(
            IServiceScopeFactory scopeFactory,
            IBackgroundJobClient backgroundJobClient,
            ILogger<DatabaseInitializationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<MolenDbContext>();

            try
            {
                _logger.LogInformation(
                    "Starting database migration and initialization.");

                if (!await dbContext.Database.CanConnectAsync(
                        cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Cannot connect to the Molen database.");
                }

                await dbContext.Database.MigrateAsync(cancellationToken);

                var startupJobId =
                    _backgroundJobClient.Enqueue<MillDatabaseImportJob>(
                        job => job.ImportAsync(
                            null!,
                            CancellationToken.None));

                _logger.LogInformation(
                    "Database initialized successfully. The Mill Database CSV " +
                    "import was queued as startup job {StartupJobId}.",
                    startupJobId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Database initialization was cancelled because the host is stopping.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Database initialization failed. The application cannot " +
                    "start safely.");

                throw;
            }
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }
}
