using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class AdminEarning
    {
        [Key]
        public int EarningID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountBDT { get; set; }

        [Required, MaxLength(50)]
        public string Source { get; set; } = string.Empty; // e.g., "SUBSCRIPTION", "RENEWAL"

        [MaxLength(100)]
        public string PackageName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}
