using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HomeownersSubdivision.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Unpaid";

        [StringLength(255)]
        public string? Notes { get; set; }

        public virtual ICollection<Payment>? Payments { get; set; }

        [NotMapped]
        public decimal PaidAmount 
        { 
            get 
            {
                if (Payments == null || !Payments.Any())
                    return 0;
                
                decimal totalPaid = 0;
                foreach (var payment in Payments)
                {
                    if (payment != null && payment.Status == "Completed")
                        totalPaid += payment.Amount;
                }
                return totalPaid;
            } 
        }

        [NotMapped]
        public decimal RemainingAmount 
        { 
            get 
            {
                return Amount - PaidAmount;
            } 
        }

        [NotMapped]
        public bool IsOverdue 
        { 
            get 
            {
                return Status != "Paid" && DueDate < DateTime.Now;
            } 
        }
    }
} 