using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class Homeowner
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Lot Number")]
        [StringLength(20)]
        public string LotNumber { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Block Number")]
        [StringLength(20)]
        public string BlockNumber { get; set; } = string.Empty;
        
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; } = DateTime.Now;
    }
} 