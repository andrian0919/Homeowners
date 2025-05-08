using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models.Analytics
{
    public enum ReportCategory
    {
        Finance,
        ServiceRequests,
        CommunityEngagement,
        UserActivity,
        Custom
    }
    
    public class ReportDefinition
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public ReportCategory Category { get; set; }
        
        [Required]
        public string QueryDefinition { get; set; } = string.Empty;
        
        public string? Parameters { get; set; }
        
        public bool IsBuiltIn { get; set; } = false;
        
        public bool IsEnabled { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int? CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }
        
        public DateTime? LastModifiedAt { get; set; }
        
        public int? LastModifiedById { get; set; }
        
        [ForeignKey("LastModifiedById")]
        public virtual User? LastModifiedBy { get; set; }
    }
} 