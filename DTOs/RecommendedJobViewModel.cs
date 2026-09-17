namespace AIResumeScreeningSystem.DTOs
{
    public class RecommendedJobViewModel
    {
        public int JobID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string? JobLocation { get; set; }
        public decimal SalaryMin { get; set; }
        public decimal SalaryMax { get; set; }
        public string JobType { get; set; } = "Full Time";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? AttachmentPath { get; set; }
        public double EstimatedMatchScore { get; set; }
    }
}
