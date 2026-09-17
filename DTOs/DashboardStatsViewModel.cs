namespace AIResumeScreeningSystem.DTOs
{
    public class DashboardStatsViewModel
    {
        public int TotalJobs { get; set; }
        public int TotalApplicants { get; set; }
        public int ShortlistedCandidates { get; set; }
        public int ScheduledInterviews { get; set; }
        public int OffersSent { get; set; }
        public int HiredCount { get; set; }
        public double AverageMatchScore { get; set; }
        public List<CandidateViewModel> RecentCandidates { get; set; } = new();


        // Dynamic Trends (computed from backend data)
        public double ApplicantTrendPercent { get; set; }
        public string ApplicantTrendDirection { get; set; } = "neutral"; // up, down, neutral
        public string ActiveJobsTrend { get; set; } = "Stable";
        public string MatchScoreTrend { get; set; } = "Stable";
        public string InterviewsTrend { get; set; } = "Stable";
        public int InterviewsNext7Days { get; set; }
    }
}
