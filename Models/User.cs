using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace HomeownersSubdivision.Models
{
    public enum UserRole
    {
        Homeowner,
        Administrator,
        Staff
    }

    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        [Required]
        public UserRole Role { get; set; } = UserRole.Homeowner;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastLoginDate { get; set; }
        
        [StringLength(255)]
        public string? ProfilePictureUrl { get; set; }
        
        // Navigation property if the user is a homeowner
        public int? HomeownerId { get; set; }
        public virtual Homeowner? Homeowner { get; set; }
        
        // Navigation property for RefundRequests
        public List<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
        
        // Navigation property for Facility Reservations
        public virtual ICollection<FacilityReservation> FacilityReservations { get; set; } = new List<FacilityReservation>();
        
        // Forum-related navigation properties
        public virtual ICollection<ForumTopic> ForumTopics { get; set; } = new List<ForumTopic>();
        public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
        public virtual ICollection<ForumReaction> ForumReactions { get; set; } = new List<ForumReaction>();
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
} 