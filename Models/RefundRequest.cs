using System;
using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class RefundRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int PaymentId { get; set; }
        
        public Payment? Payment { get; set; }
        
        public int UserId { get; set; }
        
        public User? User { get; set; }
        
        [Required(ErrorMessage = "Please select a reason for your refund request")]
        public string Reason { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Please provide details about your refund request")]
        [MinLength(10, ErrorMessage = "Please provide more information about your request")]
        public string Details { get; set; } = string.Empty;
        
        [Required]
        public string ContactPreference { get; set; } = "Email";
        
        [Phone]
        [RequiredIf(nameof(ContactPreference), "Phone", ErrorMessage = "Phone number is required when phone contact is selected")]
        public string? PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "You must accept the terms to submit a refund request")]
        [MustBeTrue(ErrorMessage = "You must accept the terms to submit a refund request")]
        public bool AcceptTerms { get; set; }
        
        public DateTime RequestDate { get; set; } = DateTime.Now;
        
        public string Status { get; set; } = "Pending";
        
        public string? AdminNotes { get; set; }
        
        public DateTime? ProcessedDate { get; set; }
        
        public string? ProcessedBy { get; set; }
    }
} 