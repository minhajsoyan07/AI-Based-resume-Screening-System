namespace AIResumeScreeningSystem.DTOs
{
    public class AdminDashboardViewModel
    {
        // Core platform stats
        public int TotalUsers          { get; set; }
        public int TotalRecruiters     { get; set; }
        public int TotalApplicants     { get; set; }
        public int ActiveJobs          { get; set; }
        public int TotalApplications   { get; set; }
        public int TotalHired          { get; set; }
        public int ScheduledInterviews { get; set; }
        public int NewUsersThisMonth   { get; set; }
        public double AverageMatchScore { get; set; }
        public double ConversionRate   { get; set; }

        // Revenue (from applicant credit purchases)
        public decimal TotalEarnings       { get; set; }  // all purchases
        public decimal EarningsThisMonth   { get; set; }  // last 30 days

        // Recent activity widgets
        public List<Models.User>              RecentUsers        { get; set; } = new();
        public List<Models.CreditTransaction> RecentTransactions { get; set; } = new();
    }
}
