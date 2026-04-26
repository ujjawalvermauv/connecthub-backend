using Microsoft.Extensions.Hosting;

namespace ConnectHub.Media.Services
{
    public class MediaCleanupHostedService : BackgroundService
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MediaCleanupHostedService> _logger;

        public MediaCleanupHostedService(IServiceScopeFactory scopeFactory, ILogger<MediaCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunCleanupAsync(stoppingToken);

            using var timer = new PeriodicTimer(CleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCleanupAsync(stoppingToken);
            }
        }

        private async Task RunCleanupAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
                await mediaService.CleanupExpiredFiles();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired media files.");
            }
        }
    }
}
