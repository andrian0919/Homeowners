using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public enum MaintenanceStatus
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

    public class MaintenanceRequest
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
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.New;
        
        [Required]
        public Priority Priority { get; set; } = Priority.Low;
        
        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }
        
        public int? AssignedToId { get; set; }
        
        // Navigation properties
        public virtual Homeowner? Homeowner { get; set; }
        public virtual User? AssignedTo { get; set; }
    }
} 