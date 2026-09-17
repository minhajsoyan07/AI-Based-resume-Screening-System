namespace AIResumeScreeningSystem.DTOs
{
    public class OfferViewModel
    {
        public int OfferID { get; set; }
        public int ApplicationID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal OfferedSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public string OfferStatus { get; set; } = string.Empty;
        public DateTime OfferDate { get; set; }
        public string? Notes { get; set; }
    }
}
