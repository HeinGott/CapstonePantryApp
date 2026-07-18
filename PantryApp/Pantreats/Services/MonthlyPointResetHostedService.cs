namespace Pantreats.Services
{
    public class MonthlyPointResetHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonthlyPointResetHostedService> _logger;

        public MonthlyPointResetHostedService(IServiceScopeFactory scopeFactory, ILogger<MonthlyPointResetHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunCatchUpResetAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextMonthlyReset(DateTimeOffset.Now);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await RunScheduledResetAsync(stoppingToken);
            }
        }

        private async Task RunCatchUpResetAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var balanceService = scope.ServiceProvider.GetRequiredService<StudentPointBalanceService>();
                await balanceService.CatchUpMonthlyResetIfNeededAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to run startup catch-up for monthly point balances.");
            }
        }

        private async Task RunScheduledResetAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var balanceService = scope.ServiceProvider.GetRequiredService<StudentPointBalanceService>();
                await balanceService.ResetBalancesForCurrentMonthAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to run scheduled monthly point balance reset.");
            }
        }

        private static TimeSpan GetDelayUntilNextMonthlyReset(DateTimeOffset now)
        {
            var nextMonth = new DateTimeOffset(
                now.Year,
                now.Month,
                1,
                0,
                0,
                5,
                now.Offset).AddMonths(1);

            if (now.Day == 1 && now.TimeOfDay < new TimeSpan(0, 0, 5))
            {
                nextMonth = new DateTimeOffset(
                    now.Year,
                    now.Month,
                    1,
                    0,
                    0,
                    5,
                    now.Offset);
            }

            return nextMonth - now;
        }
    }
}
