using System;
using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class BulkBillViewModel
    {
        [Required]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Bill Type")]
        public string BillType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public string Notes { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Unpaid";

        // Selected user IDs for bulk creation
        public int[] SelectedUsers { get; set; }
    }
} 