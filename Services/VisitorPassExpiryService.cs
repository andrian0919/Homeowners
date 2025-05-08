using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeownersSubdivision.Services
{
    public class VisitorPassExpiryService : BackgroundService
    {
        private readonly ILogger<VisitorPassExpiryService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

        public VisitorPassExpiryService(ILogger<VisitorPassExpiryService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Visitor Pass Expiry Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredPasses();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired visitor passes");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Visitor Pass Expiry Service is stopping.");
        }

        private async Task ProcessExpiredPasses()
        {
            _logger.LogInformation("Checking for expired visitor passes");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;
            
            // Get all passes that are expired but not marked as expired
            var expiredPasses = await dbContext.VisitorPasses
                .Where(p => p.ExpiryDate < now && p.Status != VisitorPassStatus.Expired)
                .ToListAsync();

            if (expiredPasses.Any())
            {
                _logger.LogInformation($"Found {expiredPasses.Count} expired visitor passes");

                foreach (var pass in expiredPasses)
                {
                    pass.Status = VisitorPassStatus.Expired;
                    pass.UpdatedAt = now;
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Updated {expiredPasses.Count} visitor passes to expired status");
            }
            else
            {
                _logger.LogInformation("No expired visitor passes found");
            }
        }
    }
} 