using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class ModelPerformanceMetrics
    {
        public int TotalPredictions { get; set; }
        public int HiredCount { get; set; }
        public int RejectedCount { get; set; }
        public int ShortlistedCount { get; set; }
        public double AverageScoreForHired { get; set; }
        public double AverageScoreForRejected { get; set; }
        public double AverageScoreForShortlisted { get; set; }
        public double AccuracyRate { get; set; }
        public double PrecisionRate { get; set; }
        public double RecallRate { get; set; }
        public double F1Score { get; set; }
        public int TotalFeedbackCount { get; set; }
        public int PositiveFeedbackCount { get; set; }
        public int NegativeFeedbackCount { get; set; }
        public int NeutralFeedbackCount { get; set; }
    }
}
