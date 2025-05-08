using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public enum VehicleType
    {
        Car,
        SUV,
        Truck,
        Van,
        Motorcycle,
        Other
    }

    public class VehicleRegistration
    {
        public int Id { get; set; }

        [Required]
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        public VehicleType Type { get; set; }

        [StringLength(20)]
        public string? Color { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(200)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 