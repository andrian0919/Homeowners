using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models.Analytics
{
    public class SavedReport
    {
        [Key]
        public int Id { get; set; }
        
        public int ReportDefinitionId { get; set; }
        
        [ForeignKey("ReportDefinitionId")]
        public virtual ReportDefinition? ReportDefinition { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        
        public int? GeneratedById { get; set; }
        
        [ForeignKey("GeneratedById")]
        public virtual User? GeneratedBy { get; set; }
        
        [Required]
        public string ResultData { get; set; } = string.Empty;
        
        public string? Parameters { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
} 