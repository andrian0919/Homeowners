using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class Event
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        
        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }
        
        [Required]
        public int CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation property
        public virtual User? Creator { get; set; }
    }
} 