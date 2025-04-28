using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public enum NotificationType
    {
        Announcement,
        MaintenanceRequest,
        Event,
        Payment,
        System
    }

    public enum NotificationStatus
    {
        Unread,
        Read,
        Archived
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum DeliveryMethod
    {
        InApp,
        Email,
        SMS,
        All
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; } = NotificationType.System;

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        [Required]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        [Required]
        public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.InApp;

        // The user receiving the notification
        [Required]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        // Optional reference to related content
        public int? RelatedItemId { get; set; }
        
        // Link to navigate to when notification is clicked
        public string? Link { get; set; }

        // When the notification was created
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // When the notification was last updated (e.g., marked as read)
        public DateTime? UpdatedAt { get; set; }

        // When the notification should be delivered
        public DateTime? ScheduledFor { get; set; }

        // Whether the notification has been sent via email/SMS
        public bool IsSent { get; set; } = false;

        // When the notification was sent via email/SMS
        public DateTime? SentAt { get; set; }
        
        // Error message if sending failed
        public string? ErrorMessage { get; set; }
        
        // Number of retry attempts
        public int RetryCount { get; set; } = 0;
    }
} 