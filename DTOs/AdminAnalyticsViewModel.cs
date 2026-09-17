namespace AIResumeScreeningSystem.DTOs
{
    // ── Admin Analytics ViewModel (replaces fragile ViewBag dynamic casting) ──

    public class AdminAnalyticsViewModel
    {
        public int TotalHired        { get; set; }
        public double AvgAIScore     { get; set; }
        public int TotalApplications { get; set; }

        public List<AppStatusCount>   AppsByStatus      { get; set; } = new();
        public List<ScoreBandCount>   ScoreDistribution { get; set; } = new();
        public List<TopJobAnalytic>   TopJobs           { get; set; } = new();
        public List<SkillDemandCount> TopSkills         { get; set; } = new();
    }

    public class AppStatusCount
    {
        public string Status { get; set; } = "";
        public int    Count  { get; set; }
    }

    public class ScoreBandCount
    {
        public string Range { get; set; } = "";
        public int    Count { get; set; }
    }

    public class TopJobAnalytic
    {
        public string JobTitle { get; set; } = "";
        public int    Count    { get; set; }
        public double AvgScore { get; set; }
    }

    public class SkillDemandCount
    {
        public string SkillName { get; set; } = "";
        public int    Count     { get; set; }
    }
}
