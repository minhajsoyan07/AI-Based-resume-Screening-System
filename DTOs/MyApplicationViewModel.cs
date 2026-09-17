namespace AIResumeScreeningSystem.DTOs
{
    public class MyApplicationViewModel
    {
        public int ApplicationID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string JobLocation { get; set; } = string.Empty;
        public decimal MatchScore { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
        public string? OfferStatus { get; set; }
        public int? OfferID { get; set; }
    }
}
