using System;
using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public enum ServiceRequestStatus
    {
        New,
        InProgress,
        Completed,
        Rejected
    }

    public enum Priority
    {
        Low = 1,
        Medium,
        High
    }

    public enum ServiceRequestType
    {
        Maintenance,
        Security,
        Cleaning,
        Plumbing,
        Electrical,
        Other
    }

    public class ServiceRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int HomeownerId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
        
        [Required]
        public Priority Priority { get; set; } = Priority.Low;
        
        [Required]
        public ServiceRequestType RequestType { get; set; } = ServiceRequestType.Maintenance;
        
        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }
        
        public int? AssignedToId { get; set; }
        
        // For tracking if updates have been made
        public DateTime? LastUpdated { get; set; }
        
        // For staff notes or updates
        public string? StaffNotes { get; set; }
        
        // For image uploads
        public string? ImageUrl { get; set; }
        
        // Navigation properties
        public virtual Homeowner? Homeowner { get; set; }
        public virtual User? AssignedTo { get; set; }
    }
} 