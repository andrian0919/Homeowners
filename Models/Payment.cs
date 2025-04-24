using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // FK to User
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // FK to Bill (optional)
        public int? BillId { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill? Bill { get; set; }

        // FK to PaymentMethod (optional)
        public int? PaymentMethodId { get; set; }

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod? PaymentMethod { get; set; }

        // Processing info
        public DateTime? ProcessedDate { get; set; }
        
        [StringLength(100)]
        public string? ProcessedBy { get; set; }

        // Refund info
        public bool IsRefunded { get; set; } = false;
        
        public DateTime? RefundDate { get; set; }
        
        [StringLength(1000)]
        public string? RefundReason { get; set; }
        
        [StringLength(100)]
        public string? RefundedBy { get; set; }
        
        [StringLength(100)]
        public string? RefundTransactionId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RefundAmount { get; set; }

        // Navigation property for refund requests
        public List<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

        [Required]
        [MaxLength(50)]
        public string ReceiptNumber { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    }
} 