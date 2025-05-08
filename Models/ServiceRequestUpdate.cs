using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class ServiceRequestUpdate
    {
        public int Id { get; set; }
        
        [Required]
        public int ServiceRequestId { get; set; }
        
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int UpdatedById { get; set; }
        
        public string? Notes { get; set; }
        
        [Required]
        public ServiceRequestStatus OldStatus { get; set; }
        
        [Required]
        public ServiceRequestStatus NewStatus { get; set; }
        
        // Navigation properties
        [ForeignKey("ServiceRequestId")]
        public virtual ServiceRequest? ServiceRequest { get; set; }
        
        [ForeignKey("UpdatedById")]
        public virtual User? UpdatedBy { get; set; }
    }
} 