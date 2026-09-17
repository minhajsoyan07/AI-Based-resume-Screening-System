using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.DTOs
{
    public class OfferCreateDTO
    {
        [Required]
        public int ApplicationID { get; set; }

        [Required]
        public decimal OfferedSalary { get; set; }

        [Required]
        public DateTime JoiningDate { get; set; }

        public string? Notes { get; set; }
    }
}
