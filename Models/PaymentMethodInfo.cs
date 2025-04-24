using System;
using System.ComponentModel.DataAnnotations;

namespace HomeownersSubdivision.Models
{
    public class PaymentMethodInfo
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [Display(Name = "Name on Card/Account")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method type is required")]
        [Display(Name = "Payment Type")]
        public PaymentMethodType Type { get; set; }

        // Credit/Debit Card fields
        [Display(Name = "Card Number")]
        [CreditCard(ErrorMessage = "Please enter a valid card number")]
        [RequiredIf(nameof(Type), PaymentMethodType.CreditCard, ErrorMessage = "Card number is required for credit cards")]
        public string? CardNumber { get; set; }

        [Display(Name = "Card Type")]
        [RequiredIf(nameof(Type), PaymentMethodType.CreditCard, ErrorMessage = "Card type is required")]
        public string? CardType { get; set; }

        [Display(Name = "Expiration Date")]
        [RequiredIf(nameof(Type), PaymentMethodType.CreditCard, ErrorMessage = "Expiration date is required")]
        public DateTime? ExpirationDate { get; set; }

        [Display(Name = "Security Code (CVV)")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3-4 digits")]
        [RequiredIf(nameof(Type), PaymentMethodType.CreditCard, ErrorMessage = "Security code is required")]
        public string? SecurityCode { get; set; }

        // Bank Account fields
        [Display(Name = "Bank Name")]
        [RequiredIf(nameof(Type), PaymentMethodType.BankAccount, ErrorMessage = "Bank name is required")]
        public string? BankName { get; set; }

        [Display(Name = "Account Number")]
        [RequiredIf(nameof(Type), PaymentMethodType.BankAccount, ErrorMessage = "Account number is required")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Routing Number")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "Routing number must be 9 digits")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Routing number must be 9 digits")]
        [RequiredIf(nameof(Type), PaymentMethodType.BankAccount, ErrorMessage = "Routing number is required")]
        public string? RoutingNumber { get; set; }

        [Display(Name = "Make this my default payment method")]
        public bool IsDefault { get; set; }

        // Helper to extract last 4 digits
        [Display(Name = "Last 4 Digits")]
        public string? LastFourDigits { get; set; }

        // Method to convert to a PaymentMethod entity
        public PaymentMethod ToPaymentMethod()
        {
            var paymentMethod = new PaymentMethod
            {
                Name = Name,
                Type = Type,
                IsDefault = IsDefault,
                CreatedDate = DateTime.Now
            };

            // Set type-specific fields
            if (Type == PaymentMethodType.CreditCard || Type == PaymentMethodType.DebitCard)
            {
                paymentMethod.CardType = CardType;
                paymentMethod.LastFourDigits = !string.IsNullOrEmpty(CardNumber) && CardNumber.Length >= 4 
                    ? CardNumber.Substring(CardNumber.Length - 4) 
                    : LastFourDigits;
                paymentMethod.ExpirationDate = ExpirationDate;
            }
            else if (Type == PaymentMethodType.BankAccount)
            {
                paymentMethod.BankName = BankName;
                paymentMethod.AccountNumber = !string.IsNullOrEmpty(AccountNumber) && AccountNumber.Length >= 4 
                    ? AccountNumber.Substring(AccountNumber.Length - 4) 
                    : LastFourDigits;
                paymentMethod.RoutingNumber = RoutingNumber;
            }

            return paymentMethod;
        }
    }
} 