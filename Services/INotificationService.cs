using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.Services
{
    public interface INotificationService
    {
        // Create a notification for a specific user
        Task<Notification> CreateNotificationAsync(
            int userId, 
            string title, 
            string message, 
            NotificationType type, 
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null);
            
        // Create a notification for a specific user with a link
        Task<Notification> CreateNotificationWithLinkAsync(
            int userId, 
            string title, 
            string message, 
            NotificationType type, 
            string link,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null);
            
        // Create notifications for all users with a specific role
        Task<int> CreateNotificationsForRoleAsync(
            UserRole role,
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null);
            
        // Create notifications for all users
        Task<int> CreateNotificationsForAllUsersAsync(
            string title,
            string message,
            NotificationType type,
            NotificationPriority priority = NotificationPriority.Normal,
            DeliveryMethod deliveryMethod = DeliveryMethod.InApp,
            int? relatedItemId = null);
            
        // Get unread notifications for a user
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId, int limit = 10);
        
        // Get all notifications for a user
        Task<List<Notification>> GetNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        
        // Mark a notification as read
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        
        // Mark all notifications as read for a user
        Task<int> MarkAllAsReadAsync(int userId);
        
        // Count unread notifications for a user
        Task<int> GetUnreadCountAsync(int userId);
        
        // Send email notification
        Task<bool> SendEmailNotificationAsync(Notification notification);
        
        // Send SMS notification
        Task<bool> SendSmsNotificationAsync(Notification notification);
        
        // Process and send pending notifications
        Task<int> ProcessPendingNotificationsAsync(int batchSize = 50);
        
        // Send notifications for multiple bills
        Task SendBulkBillNotificationsAsync(List<Bill> bills);
    }
} 