using System;

namespace AIResumeScreeningSystem.DTOs
{
    /// <summary>
    /// Analytics data shown to premium subscribers about their profile performance.
    /// </summary>
    public class ProfileAnalyticsViewModel
    {
        // ── Overview ──
        public int TotalProfileViews { get; set; }
        public int ProfileViewsThisWeek { get; set; }
        public int ProfileViewsThisMonth { get; set; }
        public int UniqueRecruiterViews { get; set; }

        // ── Search Appearances ──
        public int SearchAppearances { get; set; }
        public int SearchAppearancesThisWeek { get; set; }

        // ── Application Success ──
        public int TotalApplications { get; set; }
        public int ShortlistedCount { get; set; }
        public int InterviewedCount { get; set; }
        public int HiredCount { get; set; }
        public decimal ApplicationSuccessRate { get; set; }

        // ── Click-Through Rate ──
        public int ProfileClicksFromSearch { get; set; }
        public decimal ClickThroughRate { get; set; }

        // ── Top Skills Attracting Recruiters ──
        public List<SkillAttraction> TopAttractingSkills { get; set; } = new();

        // ── Profile Improvement Suggestions ──
        public List<ProfileSuggestion> Suggestions { get; set; } = new();

        // ── View Trend (last 7 days) ──
        public List<DailyViewCount> ViewTrend { get; set; } = new();

    }

    public class SkillAttraction
    {
        public string SkillName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public double Percentage { get; set; }
    }

    public class ProfileSuggestion
    {
        public string Section { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // High, Medium, Low
        public string Icon { get; set; } = "💡";
    }

    public class DailyViewCount
    {
        public DateTime Date { get; set; }
        public int Views { get; set; }
    }
}
