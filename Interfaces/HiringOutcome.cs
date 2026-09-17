using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class HiringOutcome
    {
        public int ApplicationID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public decimal AIPredictedScore { get; set; }
        public string ActualOutcome { get; set; } = string.Empty;
        public DateTime OutcomeDate { get; set; }
        public double ScoreDifference { get; set; }
    }
}
