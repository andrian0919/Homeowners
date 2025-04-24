using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Display(Name = "Is Published")]
        public bool IsPublished { get; set; } = true;
        
        [Display(Name = "Publish Date")]
        public DateTime PublishDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Expire Date")]
        public DateTime? ExpireDate { get; set; }
        
        [Required]
        public int CreatedBy { get; set; }
        
        // Navigation property
        public virtual User? Creator { get; set; }
    }
} 