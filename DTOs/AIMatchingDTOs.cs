using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace AIResumeScreeningSystem.DTOs
{
    public class GuestMatchRequest
    {
        public IFormFile CvFile { get; set; } = null!;
        public IFormFile? JobCircularFile { get; set; }
        public int? JobId { get; set; } // For matching against portal jobs
        public string? JobText { get; set; } // For manual text input
    }

    public class AIMatchResult
    {
        public double MatchScore { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public string Summary { get; set; } = "";
        public string Strengths { get; set; } = "";
        public List<string> ImprovementTips { get; set; } = new();
        public bool IsLocked { get; set; } = true;
    }
}
