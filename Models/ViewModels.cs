using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
        
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
    
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        public string LastName { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Lot number is required")]
        [Display(Name = "Lot Number")]
        [StringLength(20, ErrorMessage = "Lot number cannot be longer than 20 characters")]
        public string LotNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Block number is required")]
        [Display(Name = "Block Number")]
        [StringLength(20, ErrorMessage = "Block number cannot be longer than 20 characters")]
        public string BlockNumber { get; set; } = string.Empty;
    }
    
    public class BaseProfileViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        public string LastName { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
        
        [Display(Name = "Change Password")]
        public bool ChangePassword { get; set; }
        
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
        public string? ConfirmNewPassword { get; set; }
    }
    
    public class HomeownerProfileViewModel : BaseProfileViewModel
    {
        [Required(ErrorMessage = "Address is required")]
        [StringLength(100, ErrorMessage = "Address cannot be longer than 100 characters")]
        public string Address { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Lot number is required")]
        [Display(Name = "Lot Number")]
        [StringLength(20, ErrorMessage = "Lot number cannot be longer than 20 characters")]
        public string LotNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Block number is required")]
        [Display(Name = "Block Number")]
        [StringLength(20, ErrorMessage = "Block number cannot be longer than 20 characters")]
        public string BlockNumber { get; set; } = string.Empty;
        
        [Display(Name = "Join Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = false)]
        public DateTime JoinDate { get; set; }
    }
    
    public class AdminProfileViewModel : BaseProfileViewModel
    {
    }
    
    public class StaffProfileViewModel : BaseProfileViewModel
    {
    }

    public class FacilityListViewModel
    {
        public List<Facility> Facilities { get; set; } = new List<Facility>();
    }

    public class FacilityDetailsViewModel
    {
        public Facility Facility { get; set; } = null!;
        public List<FacilityReservation> UpcomingReservations { get; set; } = new List<FacilityReservation>();
    }

    public class CreateFacilityViewModel
    {
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
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(8, 0, 0); // Default: 8:00 AM
        
        [DataType(DataType.Time)]
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0); // Default: 10:00 PM
    }

    public class EditFacilityViewModel
    {
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
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan OpeningTime { get; set; }
        
        [DataType(DataType.Time)]
        public TimeSpan ClosingTime { get; set; }
        
        public bool IsActive { get; set; }
    }

    public class FacilityReservationListViewModel
    {
        public List<FacilityReservation> Reservations { get; set; } = new List<FacilityReservation>();
        public List<Facility> AvailableFacilities { get; set; } = new List<Facility>();
    }

    public class CreateReservationViewModel
    {
        [Required]
        public int FacilityId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ReservationDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Range(1, 1000)]
        public int NumberOfGuests { get; set; } = 1;

        [StringLength(500)]
        public string Purpose { get; set; } = string.Empty;
        
        public List<Facility> AvailableFacilities { get; set; } = new List<Facility>();
        
        [NotMapped]
        public string FacilityName { get; set; } = string.Empty;
        
        [NotMapped]
        public decimal HourlyRate { get; set; }
        
        [NotMapped]
        public decimal TotalCost { get; set; }
    }

    public class EditReservationViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public int FacilityId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
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
        
        public ReservationStatus Status { get; set; }
        
        public string? RejectionReason { get; set; }
        
        public List<Facility> AvailableFacilities { get; set; } = new List<Facility>();
        
        [NotMapped]
        public string FacilityName { get; set; } = string.Empty;
        
        [NotMapped]
        public decimal HourlyRate { get; set; }
        
        [NotMapped]
        public decimal TotalCost { get; set; }
    }

    public class ReservationDetailsViewModel
    {
        public FacilityReservation Reservation { get; set; } = null!;
        public User User { get; set; } = null!;
        public Facility Facility { get; set; } = null!;
    }
} 