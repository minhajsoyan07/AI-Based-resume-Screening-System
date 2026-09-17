namespace AIResumeScreeningSystem.DTOs
{
    public class ApplicantDashboardViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int HiredCount { get; set; }
        public decimal BestMatchScore { get; set; }
        public int ProfileCompletion { get; set; }
        public int UnreadNotifications { get; set; }
        public List<MyApplicationViewModel> Applications { get; set; } = new();
        public List<RecommendedJobViewModel> RecommendedJobs { get; set; } = new();
        public List<int> ActivityLast7Days { get; set; } = new();
    }
}
