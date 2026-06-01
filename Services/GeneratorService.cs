using AspireApp1.Server.Hubs;
using AspireApp1.Server.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspireApp1.Server.Services
{
    public class GeneratorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<DessertHub> _hub;
        private readonly ILogger<GeneratorService> _logger;

        private CancellationTokenSource? _cts;

        public bool IsRunning => _cts is { IsCancellationRequested: false };

        public GeneratorService(
            IServiceScopeFactory scopeFactory,
            IHubContext<DessertHub> hub,
            ILogger<GeneratorService> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        public void Start(int batchSize = 5, int intervalMs = 3000)
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            // Fresh scope per iteration so each batch gets its own AppDbContext.
                            // Concrete repos are resolved directly to bypass the Logging* decorators —
                            // generator traffic must not pollute the audit log.
                            using var scope = _scopeFactory.CreateScope();
                            var faker = scope.ServiceProvider.GetRequiredService<FakerService>();
                            var dessertRepo = scope.ServiceProvider.GetRequiredService<DessertRepository>();
                            var orderRepo = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                            var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();

                            var dessertBatch = faker.GenerateDessertBatch(batchSize);
                            foreach (var dessert in dessertBatch)
                                dessertRepo.Add(dessert);

                            var allDesserts = dessertRepo.GetAll();
                            var allUsers = userRepo.GetAll();
                            var orderBatch = faker.GenerateOrderBatch(allDesserts, allUsers, batchSize);
                            foreach (var order in orderBatch)
                                orderRepo.Add(order);

                            await _hub.Clients.All.SendAsync("ReceiveBatch", dessertBatch, orderBatch, token);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Generator iteration failed; continuing");
                        }

                        try
                        {
                            await Task.Delay(intervalMs, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _cts = null;
                }
            }, token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }
    }
}
