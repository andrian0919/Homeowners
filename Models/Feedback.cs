using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public enum FeedbackType
    {
        Feedback,
        Complaint,
        Suggestion
    }

    public enum FeedbackStatus
    {
        Pending,
        InProgress,
        Resolved,
        Rejected,
        Closed
    }

    public class Feedback
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Type")]
        public FeedbackType Type { get; set; } = FeedbackType.Feedback;
        
        [Required]
        [Display(Name = "Status")]
        public FeedbackStatus Status { get; set; } = FeedbackStatus.Pending;
        
        [Display(Name = "Category")]
        [StringLength(100)]
        public string? Category { get; set; }
        
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 0; // 0 = Low, 1 = Medium, 2 = High
        
        // Dates
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
        
        [Display(Name = "Resolved At")]
        public DateTime? ResolvedAt { get; set; }
        
        // Response from admin/staff
        [StringLength(2000)]
        [Display(Name = "Admin Response")]
        public string? AdminResponse { get; set; }
        
        // Who submitted the feedback
        [Required]
        public int SubmittedById { get; set; }
        
        [ForeignKey("SubmittedById")]
        public virtual User? SubmittedBy { get; set; }
        
        // Admin/staff who processed the feedback
        public int? ProcessedById { get; set; }
        
        [ForeignKey("ProcessedById")]
        public virtual User? ProcessedBy { get; set; }
        
        // Associated homeowner (if applicable)
        public int? HomeownerId { get; set; }
        
        [ForeignKey("HomeownerId")]
        public virtual Homeowner? Homeowner { get; set; }
        
        // If feedback is public or private (visible to other homeowners)
        [Display(Name = "Is Public")]
        public bool IsPublic { get; set; } = false;
        
        // Additional data
        [Display(Name = "Attachments")]
        public string? AttachmentUrls { get; set; }
    }
} 