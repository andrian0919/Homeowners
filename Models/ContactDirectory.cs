using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public enum ContactCategory
    {
        HomewownersAssociation,
        SecurityOffice,
        MaintenanceTeam,
        Management,
        EmergencyServices,
        Utilities,
        Other
    }

    public class ContactDirectory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        public ContactCategory Category { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Office { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Working hours
        public string? WorkingHours { get; set; }

        // For priority sorting
        public int SortOrder { get; set; } = 0;

        // Visibility restrictions (bit flags for roles)
        public int VisibleToRoles { get; set; } = 7; // Default visible to all roles (Admin=1, Staff=2, Homeowner=4)

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedById { get; set; }
        
        [ForeignKey("UpdatedById")]
        public virtual User? UpdatedBy { get; set; }

        // Helper method to check if visible to a specific role
        public bool IsVisibleToRole(UserRole role)
        {
            int roleFlag = 0;
            
            switch(role)
            {
                case UserRole.Administrator:
                    roleFlag = 1;
                    break;
                case UserRole.Staff:
                    roleFlag = 2;
                    break;
                case UserRole.Homeowner:
                    roleFlag = 4;
                    break;
            }
            
            return (VisibleToRoles & roleFlag) != 0;
        }

        // Helper method to set visibility for a specific role
        public void SetVisibilityForRole(UserRole role, bool isVisible)
        {
            int roleFlag = 0;
            
            switch(role)
            {
                case UserRole.Administrator:
                    roleFlag = 1;
                    break;
                case UserRole.Staff:
                    roleFlag = 2;
                    break;
                case UserRole.Homeowner:
                    roleFlag = 4;
                    break;
            }
            
            if (isVisible)
            {
                VisibleToRoles |= roleFlag; // Set the bit
            }
            else
            {
                VisibleToRoles &= ~roleFlag; // Clear the bit
            }
        }
    }
} 