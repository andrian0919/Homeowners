using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public enum ReservationStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Completed
    }

    public class FacilityReservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReservationDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Range(1, 1000)]
        public int NumberOfGuests { get; set; }

        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        public string? RejectionReason { get; set; }

        [Range(0, 100000)]
        public decimal TotalCost { get; set; }

        [NotMapped]
        public TimeSpan Duration => EndTime - StartTime;
    }
} 