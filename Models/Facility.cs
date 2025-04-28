using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public enum FacilityType
    {
        FunctionHall,
        SportsCourt,
        SwimmingPool,
        Other
    }

    public class Facility
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FacilityType Type { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int MaxCapacity { get; set; }

        [Range(0, 10000)]
        public decimal HourlyRate { get; set; }

        public TimeSpan OpeningTime { get; set; } = new TimeSpan(8, 0, 0); // Default: 8:00 AM
        
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0); // Default: 10:00 PM
        
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<FacilityReservation> Reservations { get; set; } = new List<FacilityReservation>();
    }
} 