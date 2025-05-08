using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models.Analytics
{
    public enum ParameterType
    {
        String,
        DateTime,
        Integer,
        Decimal,
        Boolean,
        Enum
    }
    
    public class ReportParameter
    {
        [Key]
        public int Id { get; set; }
        
        public int ReportDefinitionId { get; set; }
        
        [ForeignKey("ReportDefinitionId")]
        public virtual ReportDefinition? ReportDefinition { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Label { get; set; } = string.Empty;
        
        [Required]
        public ParameterType Type { get; set; }
        
        public string? DefaultValue { get; set; }
        
        public string? ValidationRules { get; set; }
        
        public string? Options { get; set; }
        
        public bool IsRequired { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
    }
} 