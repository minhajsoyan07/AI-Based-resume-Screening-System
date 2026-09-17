using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class ResumeMatchResult
    {
        [JsonPropertyName("MatchScore")]
        public int MatchScore { get; set; }

        [JsonPropertyName("CoreSkillMatch")]
        public List<string> CoreSkillMatch { get; set; } = new List<string>();

        [JsonPropertyName("MissingSkills")]
        public List<string> MissingSkills { get; set; } = new List<string>();

        [JsonPropertyName("Strengths")]
        public string Strengths { get; set; } = string.Empty;

        [JsonPropertyName("Recommendation")]
        public string Recommendation { get; set; } = string.Empty;

        [JsonPropertyName("Verdict")]
        public string Verdict { get; set; } = string.Empty;

        [JsonPropertyName("Justification")]
        public string Justification { get; set; } = string.Empty;

        // ── Detailed Score Breakdown ──
        public int SkillScore { get; set; }
        public int ExperienceScore { get; set; }
        public int EducationScore { get; set; }
        public int KeywordScore { get; set; }
        public int CertificationScore { get; set; }
        public int CareerGrowthScore { get; set; }

        // ── Hard Requirement Details (for result UI) ──
        public bool EducationMet { get; set; }
        public bool ExperienceMet { get; set; }
        public bool SubjectMet { get; set; }

        public string RequiredDegree { get; set; } = string.Empty;
        public string CandidateDegree { get; set; } = string.Empty;
        public int RequiredExperienceYears { get; set; }
        public int CandidateExperienceYears { get; set; }
        public string RequiredSubject { get; set; } = string.Empty;
        public string CandidateSubject { get; set; } = string.Empty;

        // ── Validation details ──
        public bool IsValid { get; set; } = true;
        public string ValidationError { get; set; } = string.Empty;
        public string AIProvider { get; set; } = string.Empty;
        public string AIModel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Internal DTO returned by the AI evaluation phase.
    /// </summary>
    public class AIResumeEvaluationResult
    {
        [JsonPropertyName("final_score")]
        public double FinalScore { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("reasons")]
        public List<string> Reasons { get; set; } = new();

        [JsonPropertyName("validation")]
        public AIValidation Validation { get; set; } = new();

        [JsonPropertyName("hard_requirements")]
        public AIHardRequirements HardRequirements { get; set; } = new();

        [JsonPropertyName("scores")]
        public AIScores Scores { get; set; } = new();

        [JsonPropertyName("extracted_data")]
        public AIExtractedData ExtractedData { get; set; } = new();

        [JsonIgnore]
        public string SucceededProvider { get; set; } = string.Empty;

        [JsonIgnore]
        public string SucceededModel { get; set; } = string.Empty;
    }

    public class AIValidation
    {
        [JsonPropertyName("is_resume_valid")]
        public bool IsResumeValid { get; set; }

        [JsonPropertyName("is_job_description_valid")]
        public bool IsJobDescriptionValid { get; set; }
    }

    public class AIHardRequirements
    {
        [JsonPropertyName("education_met")]
        public bool EducationMet { get; set; }

        [JsonPropertyName("experience_met")]
        public bool ExperienceMet { get; set; }

        [JsonPropertyName("subject_met")]
        public bool SubjectMet { get; set; }

        // ── Extracted comparison data ──
        [JsonPropertyName("required_degree")]
        public string RequiredDegree { get; set; } = string.Empty;

        [JsonPropertyName("candidate_degree")]
        public string CandidateDegree { get; set; } = string.Empty;

        [JsonPropertyName("required_experience_years")]
        public int RequiredExperienceYears { get; set; }

        [JsonPropertyName("candidate_experience_years")]
        public int CandidateExperienceYears { get; set; }

        [JsonPropertyName("required_subject")]
        public string RequiredSubject { get; set; } = string.Empty;

        [JsonPropertyName("candidate_subject")]
        public string CandidateSubject { get; set; } = string.Empty;
    }

    public class AIScores
    {
        [JsonPropertyName("education")]
        public double Education { get; set; }

        [JsonPropertyName("experience")]
        public double Experience { get; set; }

        [JsonPropertyName("skills")]
        public double Skills { get; set; }

        [JsonPropertyName("keywords")]
        public double Keywords { get; set; }
    }

    public class AIExtractedData
    {
        [JsonPropertyName("resume_skills")]
        public List<string> ResumeSkills { get; set; } = new();

        [JsonPropertyName("job_skills")]
        public List<string> JobSkills { get; set; } = new();

        [JsonPropertyName("core_skill_match")]
        public List<string> CoreSkillMatch { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("strengths")]
        public string Strengths { get; set; } = string.Empty;

        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }
}
