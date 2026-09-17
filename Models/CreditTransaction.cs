using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class CreditTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int CreditsAmount { get; set; } // Changed from CreditsUsed for clarity

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountBDT { get; set; }

        [Required, MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // e.g., "SCAN", "BONUS", "PURCHASE"

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? TransactionReference { get; set; } // For bKash/Nagad TrxID

        public bool AdminVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}
