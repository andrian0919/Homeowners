using HomeownersSubdivision.Models;
using HomeownersSubdivision.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace HomeownersSubdivision.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null)
        {
            try
            {
                // Create new notification
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    DeliveryMethod = deliveryMethod,
                    RelatedItemId = relatedItemId,
                    Status = NotificationStatus.Unread,
                    CreatedAt = DateTime.Now
                };

                // Add to database
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // If the notification should be sent immediately via email or SMS
                if (deliveryMethod == DeliveryMethod.Email || deliveryMethod == DeliveryMethod.SMS || deliveryMethod == DeliveryMethod.All)
                {
                    // Process this notification immediately for email/SMS delivery
                    if (deliveryMethod == DeliveryMethod.Email || deliveryMethod == DeliveryMethod.All)
                    {
                        await SendEmailNotificationAsync(notification);
                    }

                    if (deliveryMethod == DeliveryMethod.SMS || deliveryMethod == DeliveryMethod.All)
                    {
                        await SendSmsNotificationAsync(notification);
                    }
                }

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task<Notification> CreateNotificationWithLinkAsync(
            int userId,
            string title,
            string message,
            NotificationType type,
            string link,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null)
        {
            try
            {
                // Create new notification
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Priority = priority,
                    DeliveryMethod = deliveryMethod,
                    RelatedItemId = relatedItemId,
                    Link = link,
                    Status = NotificationStatus.Unread,
                    CreatedAt = DateTime.Now
                };

                // Add to database
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // If the notification should be sent immediately via email or SMS
                if (deliveryMethod == DeliveryMethod.Email || deliveryMethod == DeliveryMethod.SMS || deliveryMethod == DeliveryMethod.All)
                {
                    // Process this notification immediately for email/SMS delivery
                    if (deliveryMethod == DeliveryMethod.Email || deliveryMethod == DeliveryMethod.All)
                    {
                        await SendEmailNotificationAsync(notification);
                    }

                    if (deliveryMethod == DeliveryMethod.SMS || deliveryMethod == DeliveryMethod.All)
                    {
                        await SendSmsNotificationAsync(notification);
                    }
                }

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification with link for user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task<int> CreateNotificationsForRoleAsync(
            UserRole role,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null)
        {
            try
            {
                // Get all users with specified role
                var users = await _context.Users
                    .Where(u => u.Role == role && u.IsActive)
                    .ToListAsync();

                int notificationsCreated = 0;

                // Create a notification for each user
                foreach (var user in users)
                {
                    await CreateNotificationAsync(
                        user.Id,
                        title,
                        message,
                        type,
                        priority,
                        deliveryMethod,
                        relatedItemId);

                    notificationsCreated++;
                }

                return notificationsCreated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notifications for role {Role}: {Message}", role, ex.Message);
                throw;
            }
        }

        public async Task<int> CreateNotificationsForAllUsersAsync(
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null)
        {
            try
            {
                // Get all active users
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .ToListAsync();

                int notificationsCreated = 0;

                // Create a notification for each user
                foreach (var user in users)
                {
                    await CreateNotificationAsync(
                        user.Id,
                        title,
                        message,
                        type,
                        priority,
                        deliveryMethod,
                        relatedItemId);

                    notificationsCreated++;
                }

                return notificationsCreated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notifications for all users: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId, int limit = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            notification.Status = NotificationStatus.Read;
            notification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return 0;
            }

            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return unreadNotifications.Count;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.Status == NotificationStatus.Unread);
        }

        public async Task<bool> SendEmailNotificationAsync(Notification notification)
        {
            try
            {
                // Get user's email address
                var user = await _context.Users.FindAsync(notification.UserId);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send email notification {NotificationId} - user not found or no email", notification.Id);
                    return false;
                }

                // Get email configuration from app settings
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var password = _configuration["EmailSettings:Password"];

                // For development, you might want to log the email instead of sending it
                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
                {
                    _logger.LogInformation("Email would be sent to {Email}: {Title} - {Message}", 
                        user.Email, notification.Title, notification.Message);
                    
                    // Update notification as sent even though we're just logging
                    notification.IsSent = true;
                    notification.SentAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    return true;
                }

                // Create and configure the email message
                var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = notification.Title,
                    Body = notification.Message,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(user.Email, $"{user.FirstName} {user.LastName}"));

                // Setup SMTP client
                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, password);

                    // Send the email
                    await client.SendMailAsync(message);
                }

                // Update notification as sent
                notification.IsSent = true;
                notification.SentAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification {NotificationId}: {Message}", notification.Id, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendSmsNotificationAsync(Notification notification)
        {
            try
            {
                // Get user's phone number
                var user = await _context.Users.FindAsync(notification.UserId);
                if (user == null || string.IsNullOrEmpty(user.PhoneNumber))
                {
                    _logger.LogWarning("Cannot send SMS notification {NotificationId} - user not found or no phone number", notification.Id);
                    return false;
                }

                // Get SMS configuration from app settings
                var accountSid = _configuration["SmsSettings:AccountSid"];
                var authToken = _configuration["SmsSettings:AuthToken"];
                var fromNumber = _configuration["SmsSettings:FromNumber"];

                // For development, you might want to log the SMS instead of sending it
                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
                {
                    _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", 
                        user.PhoneNumber, notification.Message);
                    
                    // Update notification as sent even though we're just logging
                    notification.IsSent = true;
                    notification.SentAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    return true;
                }

                // In a real implementation, you would use a service like Twilio, Nexmo, or AWS SNS
                // Here we'll just simulate sending the SMS and log it

                _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", 
                    user.PhoneNumber, notification.Message);

                // Update notification as sent
                notification.IsSent = true;
                notification.SentAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification {NotificationId}: {Message}", notification.Id, ex.Message);
                return false;
            }
        }

        public async Task<int> ProcessPendingNotificationsAsync(int batchSize = 50)
        {
            // Get pending notifications
            var pendingNotifications = await _context.Notifications
                .Where(n => !n.IsSent 
                        && (n.DeliveryMethod == DeliveryMethod.Email 
                            || n.DeliveryMethod == DeliveryMethod.SMS 
                            || n.DeliveryMethod == DeliveryMethod.All)
                        && n.CreatedAt < DateTime.Now)
                .OrderBy(n => n.CreatedAt)
                .Take(batchSize)
                .ToListAsync();

            int processedCount = 0;

            // Process each notification
            foreach (var notification in pendingNotifications)
            {
                try
                {
                    bool success = false;

                    // Send via appropriate channels
                    if (notification.DeliveryMethod == DeliveryMethod.Email || notification.DeliveryMethod == DeliveryMethod.All)
                    {
                        success = await SendEmailNotificationAsync(notification);
                    }

                    if (notification.DeliveryMethod == DeliveryMethod.SMS || notification.DeliveryMethod == DeliveryMethod.All)
                    {
                        success = await SendSmsNotificationAsync(notification);
                    }

                    // Update notification status
                    notification.IsSent = success;
                    notification.SentAt = success ? DateTime.Now : null;
                    notification.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification {NotificationId}: {Message}", notification.Id, ex.Message);
                    
                    // Update error information
                    notification.ErrorMessage = ex.Message;
                    notification.RetryCount++;
                    notification.UpdatedAt = DateTime.Now;
                    
                    await _context.SaveChangesAsync();
                }
            }

            return processedCount;
        }
        
        public async Task SendBulkBillNotificationsAsync(List<Bill> bills)
        {
            if (bills == null || !bills.Any())
            {
                _logger.LogWarning("Cannot send bulk bill notifications - no bills provided");
                return;
            }
            
            try
            {
                foreach (var bill in bills)
                {
                    // Skip bills with no user assigned
                    if (bill.UserId <= 0)
                    {
                        _logger.LogWarning("Cannot send bill notification for bill {BillId} - no user assigned", bill.Id);
                        continue;
                    }
                    
                    // Create notification for this bill
                    await CreateNotificationAsync(
                        bill.UserId,
                        "New Bill Created",
                        $"A new bill has been created: {bill.Description} for ${bill.Amount:N2}. Due date: {bill.DueDate:MM/dd/yyyy}",
                        NotificationType.Payment,
                        NotificationPriority.Normal,
                        DeliveryMethod.All,
                        bill.Id
                    );
                }
                
                _logger.LogInformation("Successfully sent {Count} bulk bill notifications", bills.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk bill notifications: {Message}", ex.Message);
                throw;
            }
        }
    }
} 