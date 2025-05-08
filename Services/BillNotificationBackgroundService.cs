using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HomeownersSubdivision.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.Services
{
    public class BillNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BillNotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check once per day

        public BillNotificationBackgroundService(
            IServiceProvider services,
            ILogger<BillNotificationBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForDueBillsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for due bills");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckForDueBillsAsync()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.Today;
            var sevenDaysFromNow = today.AddDays(7);

            // Get all unpaid bills that are due within the next 7 days or are overdue
            var dueBills = await dbContext.Bills
                .Include(b => b.User)
                .Where(b => b.Status != "Paid" && b.Status != "Cancelled" && b.DueDate <= sevenDaysFromNow)
                .ToListAsync();

            foreach (var bill in dueBills)
            {
                string title;
                string message;
                Models.NotificationPriority priority;
                string link = $"/Billing/MyBills"; // Link to My Bills page

                if (bill.DueDate < today)
                {
                    // Overdue bill
                    int daysOverdue = (int)(today - bill.DueDate).TotalDays;
                    title = "Overdue Bill";
                    message = $"Your payment of ${bill.Amount:N2} for {bill.Description} is overdue by {daysOverdue} day(s). Please make your payment as soon as possible to avoid additional charges.";
                    priority = Models.NotificationPriority.High;
                }
                else if (bill.DueDate == today)
                {
                    // Due today
                    title = "Bill Due Today";
                    message = $"Your payment of ${bill.Amount:N2} for {bill.Description} is due today. Please make your payment to avoid late fees.";
                    priority = Models.NotificationPriority.High;
                }
                else
                {
                    // Due within 7 days
                    int daysUntilDue = (int)(bill.DueDate - today).TotalDays;
                    title = "Upcoming Bill Due";
                    message = $"Your payment of ${bill.Amount:N2} for {bill.Description} is due in {daysUntilDue} day(s).";
                    priority = Models.NotificationPriority.Normal;
                }

                // Create notification with link to bill details page
                await notificationService.CreateNotificationWithLinkAsync(
                    bill.UserId,
                    title,
                    message,
                    Models.NotificationType.Payment,
                    link,
                    priority,
                    Models.DeliveryMethod.All,
                    bill.Id
                );

                // Update bill status to "Overdue" if it's past due date
                if (bill.DueDate < today && bill.Status == "Unpaid")
                {
                    bill.Status = "Overdue";
                    dbContext.Update(bill);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
} 