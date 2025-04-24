using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public PaymentMethodType Type { get; set; }

        [StringLength(4)]
        public string? LastFourDigits { get; set; }

        [StringLength(50)]
        public string? CardType { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [StringLength(50)]
        public string? BankName { get; set; }

        [StringLength(20)]
        public string? AccountNumber { get; set; }

        [StringLength(50)]
        public string? RoutingNumber { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastUsedDate { get; set; }

        // Card-specific properties
        [StringLength(100)]
        public string? CardHolderName { get; set; }

        [StringLength(19)]
        public string? CardNumber { get; set; }

        [StringLength(2)]
        public string? ExpirationMonth { get; set; }

        [StringLength(4)]
        public string? ExpirationYear { get; set; }

        [StringLength(4)]
        public string? Cvv { get; set; }

        // Bank-specific properties
        [StringLength(100)]
        public string? AccountHolderName { get; set; }

        // Billing Address
        [StringLength(100)]
        public string? NickName { get; set; }

        [StringLength(200)]
        public string? BillingAddress { get; set; }

        [StringLength(100)]
        public string? BillingCity { get; set; }

        [StringLength(2)]
        public string? BillingState { get; set; }

        [StringLength(10)]
        public string? BillingZip { get; set; }

        [StringLength(2)]
        public string? BillingCountry { get; set; }

        // Card last four digits for display
        [StringLength(4)]
        public string? CardLastFour { get; set; }

        [NotMapped]
        public string DisplayName 
        { 
            get 
            {
                switch (Type)
                {
                    case PaymentMethodType.CreditCard:
                    case PaymentMethodType.DebitCard:
                        return $"{Name} •••• {LastFourDigits}";
                    case PaymentMethodType.BankAccount:
                        return $"{BankName} Account •••• {AccountNumber}";
                    case PaymentMethodType.GCash:
                        return $"GCash •••• {AccountNumber?.Substring(Math.Max(0, AccountNumber.Length - 4))}";
                    default:
                        return Name;
                }
            } 
        }
    }

    public enum PaymentMethodType
    {
        CreditCard,
        DebitCard,
        BankAccount,
        GCash,
        Other
    }
} 