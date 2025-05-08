using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models.Analytics
{
    public enum WidgetType
    {
        Counter,
        LineChart,
        BarChart,
        PieChart,
        Table,
        CustomHtml
    }
    
    public enum WidgetSize
    {
        Small,      // 1/4 width
        Medium,     // 1/2 width
        Large,      // 3/4 width
        ExtraLarge  // Full width
    }
    
    public class DashboardWidget
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public WidgetType Type { get; set; }
        
        [Required]
        public WidgetSize Size { get; set; } = WidgetSize.Medium;
        
        public int SortOrder { get; set; } = 0;
        
        public string? Configuration { get; set; }
        
        public int? ReportDefinitionId { get; set; }
        
        [ForeignKey("ReportDefinitionId")]
        public virtual ReportDefinition? ReportDefinition { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int? CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }
        
        // Specifies what UserRoles can see this widget
        public int VisibleToRoles { get; set; } = 1; // Default visible to only Admin
    }
} 