using Microsoft.Extensions.Options;

namespace AspireApp1.Server.Security
{
    public class ThreatDetectorWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IOptionsMonitor<SecurityOptions> _opts;
        private readonly ILogger<ThreatDetectorWorker> _logger;

        public ThreatDetectorWorker(
            IServiceProvider services,
            IOptionsMonitor<SecurityOptions> opts,
            ILogger<ThreatDetectorWorker> logger)
        {
            _services = services;
            _opts = opts;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ThreatDetectorWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "ThreatDetectorWorker tick failed.");
                }

                int delay = Math.Max(1, _opts.CurrentValue.ScanIntervalSeconds);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("ThreatDetectorWorker stopping.");
        }

        public async Task RunOnceAsync(CancellationToken ct)
        {
            using IServiceScope scope = _services.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IEnumerable<IThreatRule> rules =
                scope.ServiceProvider.GetServices<IThreatRule>();

            DateTime now = DateTime.UtcNow;

            foreach (IThreatRule rule in rules)
            {
                try
                {
                    await rule.EvaluateAsync(db, now, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "Threat rule {RuleName} failed on this tick.",
                        rule.Name);
                }
            }
        }
    }
}
