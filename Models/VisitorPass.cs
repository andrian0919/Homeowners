using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public enum VisitorPassStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired
    }

    public class VisitorPass
    {
        public int Id { get; set; }

        [Required]
        public int RequestedById { get; set; }
        public virtual User RequestedBy { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string VisitorName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string VisitorContact { get; set; } = string.Empty;

        [Required]
        public DateTime VisitDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [StringLength(200)]
        public string? Purpose { get; set; }

        [Required]
        public VisitorPassStatus Status { get; set; } = VisitorPassStatus.Pending;

        public int? ApprovedById { get; set; }
        public virtual User? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 