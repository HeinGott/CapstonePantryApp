using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Services
{
    public class StudentPointBalanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentPointBalanceService> _logger;

        public StudentPointBalanceService(ApplicationDbContext context, ILogger<StudentPointBalanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ResetBalancesForCurrentMonthAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var currentMonthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var latestApplications = await _context.UserApplications
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .ToListAsync(cancellationToken);

            var latestApplicationsByUser = latestApplications
                .GroupBy(application => application.UserId)
                .Select(group => group.First())
                .ToList();

            var updatedCount = 0;

            foreach (var application in latestApplicationsByUser)
            {
                if (application.LastPointResetAt.HasValue &&
                    application.LastPointResetAt.Value.Year == currentMonthStart.Year &&
                    application.LastPointResetAt.Value.Month == currentMonthStart.Month)
                {
                    continue;
                }

                if (application.ApplicationStatus == ApplicationStatuses.Approved &&
                    application.MonthlyPointBalance.HasValue)
                {
                    application.CurrentPointBalance = application.MonthlyPointBalance.Value;
                    application.LastPointResetAt = utcNow;
                }
                else
                {
                    application.CurrentPointBalance = null;
                    application.LastPointResetAt = null;
                }
                updatedCount++;
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Reset monthly point balances for {UpdatedCount} student application records.", updatedCount);
            }

            return updatedCount;
        }

        public async Task<int> CatchUpMonthlyResetIfNeededAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var currentMonthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var resetAlreadyApplied = await _context.UserApplications
                .AnyAsync(application =>
                    application.LastPointResetAt.HasValue &&
                    application.LastPointResetAt.Value.Year == currentMonthStart.Year &&
                    application.LastPointResetAt.Value.Month == currentMonthStart.Month,
                    cancellationToken);

            if (resetAlreadyApplied)
            {
                return 0;
            }

            return await ResetBalancesForCurrentMonthAsync(cancellationToken);
        }
    }
}
