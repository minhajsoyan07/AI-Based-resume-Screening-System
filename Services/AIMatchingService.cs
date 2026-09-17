using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.DTOs;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Google.GenAI;
using Google.GenAI.Types;

namespace AIResumeScreeningSystem.Services
{
    public class JobRequirements
    {
        public string RequiredDegreeLevel { get; set; } = "";
        public List<string> RequiredMajors { get; set; } = new();
        public int MinimumYearsOfExperience { get; set; } = 0;
        public List<string> MustHaveSkills { get; set; } = new();
    }

    public class AIMatchingService : IAIMatchingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AIMatchingService> _logger;

        private static readonly ConcurrentDictionary<string, (DateTime Timestamp, decimal Score)> _scoreCache = new();
        private static readonly ConcurrentDictionary<int, JobRequirements> _jobReqCache = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        private static readonly Lazy<HashSet<string>> _knownCertifications = new(() => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AWS Certified", "Azure Certified", "Google Cloud Certified", "PMP", "CISSP", "CCNA", "CCNP", "CCIE",
            "MCSA", "MCSE", "MCSD", "MCP", "Scrum Master", "CSM", "CSPO", "CompTIA A+", "CompTIA Security+",
            "CompTIA Network+", "ITIL", "Six Sigma", "CPA", "CFA", "Oracle Certified", "Salesforce Certified",
            "Kubernetes", "Docker Certified", "Terraform", "CEH", "OSCP", "CISM", "CRISC", "Red Hat Certified",
            "RHCE", "RHCSA", "Certified Data Professional", "CDP", "Machine Learning Certification"
        });

        private static readonly Lazy<Dictionary<string, List<string>>> _techSynonyms = new(() => new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "C#", new() { "CSharp", ".NET", "DotNet", "ASP.NET", "ASP.NET Core", "Entity Framework", "Blazor", "LINQ" } },
            { "JavaScript", new() { "JS", "ES6", "NodeJS", "Node.js", "Vanilla JS", "React", "Vue", "Angular" } },
            { "Python", new() { "Py", "Django", "Flask", "Pandas", "NumPy", "PyTorch", "FastAPI", "Jupyter" } },
            { "SQL", new() { "TSQL", "MySQL", "PostgreSQL", "SQL Server", "NoSQL", "Oracle", "MongoDB" } },
            { "Azure", new() { "Microsoft Cloud", "Entra", "App Service", "Azure DevOps", "Azure Functions" } },
            { "AWS", new() { "Amazon Web Services", "EC2", "S3", "Lambda", "DynamoDB", "ECS", "EKS", "RDS" } },
            { "GCP", new() { "Google Cloud", "BigQuery", "Cloud Run", "Cloud Functions", "GKE" } },
            { "Docker", new() { "Containers", "Kubernetes", "K8s", "container", "docker-compose", "Podman", "Helm" } },
            { "React", new() { "ReactJS", "React.js", "Redux", "React Router", "JSX", "Next.js", "Remix" } },
            { "Java", new() { "Spring", "Spring Boot", "Spring MVC", "Hibernate", "Maven", "Gradle", "Quarkus" } },
            { "Machine Learning", new() { "ML", "AI", "Deep Learning", "Neural Network", "NLP", "Computer Vision" } },
            { "DevOps", new() { "CI/CD", "Jenkins", "GitHub Actions", "GitLab CI", "CircleCI", "IaC", "GitOps" } }
        });

        private static readonly Lazy<Dictionary<string, int>> _educationLevelMap = new(() => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"High School", 1}, {"HSC", 1}, {"GED", 1}, {"Associate", 2}, {"Diploma", 2}, {"Foundation", 2},
            {"Bachelor", 3}, {"BSc", 3}, {"BBA", 3}, {"B.Eng", 3}, {"B.Tech", 3}, {"BE", 3}, {"BS", 3},
            {"Master", 4}, {"MSc", 4}, {"MBA", 4}, {"M.Tech", 4}, {"MS", 4}, {"ME", 4}, {"MEng", 4},
            {"PhD", 5}, {"Doctorate", 5}, {"MD", 5}, {"JD", 5}, {"DBA", 5}
        });

        private const double SkillWeight = 25;
        private const double ExperienceWeight = 30;
        private const double EducationWeight = 30;
        private const double KeywordWeight = 10;
        private const double SemanticWeight = 0;
        private const double CertificationBonus = 3;
        private const double CareerGrowthWeight = 2;
        private const double IndustryWeight = 0;

        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AIMatchingService(ApplicationDbContext db, ILogger<AIMatchingService> logger, IConfiguration config, HttpClient httpClient)
        {
            _db = db;
            _logger = logger;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<decimal> CalculateMatchScoreAsync(int applicationId)
        {
            var sw = Stopwatch.StartNew();
            var cacheKey = $"score_{applicationId}";

            if (_scoreCache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.Timestamp < CacheDuration)
            {
                return cached.Score;
            }

            var application = await _db.Applications
                .Include(a => a.Job).ThenInclude(j => j.JobSkills).ThenInclude(js => js.Skill)
                .Include(a => a.Applicant).ThenInclude(ap => ap!.ApplicantSkills).ThenInclude(s => s.Skill)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application == null) return 0;

            var job = application.Job;
            var applicant = application.Applicant;
            
            if (job == null || applicant == null) return 0;

            var candidateSkills = applicant.ApplicantSkills?.Select(s => s.Skill.SkillName).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resumeText = (applicant.ProfileSummary ?? "").ToLower();

            var jobReqs = await GetOrExtractJobRequirementsAsync(job);

            var skillScore = CalculateSkillScoreOptimized(job.RequiredSkills ?? "", candidateSkills, resumeText);
            var expScore = CalculateExperienceScore(Math.Max(job.MinimumExperience, jobReqs.MinimumYearsOfExperience), applicant.YearsOfExperience);
            var eduScore = CalculateEducationScoreInternal(job.EducationLevel, applicant.HighestEducation, jobReqs);
            var kwScore = CalculateKeywordScore(job.JobDescription ?? "", resumeText);
            var certScore = CalculateCertificationScore(resumeText);
            var careerScore = CalculateCareerGrowthScore(resumeText);
            var industryScore = CalculateIndustryMatchScore(job.JobDescription ?? "", resumeText);

            // ── ADVANCED SEMANTIC ALIGNMENT ──
            decimal semanticAlignment = 75; // Default to neutral-high

            // ── HARD REQUIREMENTS CHECK ──
            List<string> missingReqs = new();
            
            if (jobReqs.MinimumYearsOfExperience > applicant.YearsOfExperience)
                missingReqs.Add($"Requires {jobReqs.MinimumYearsOfExperience} years experience (has {applicant.YearsOfExperience})");

            string requiredEduStr = !string.IsNullOrEmpty(jobReqs.RequiredDegreeLevel) ? jobReqs.RequiredDegreeLevel : job.EducationLevel ?? "";

            if (!string.IsNullOrEmpty(requiredEduStr))
            {
                var reqLvl = GetEducationLevel(requiredEduStr);
                var candLvl = GetEducationLevel(applicant.HighestEducation ?? "");
                if (reqLvl > 0 && candLvl < reqLvl)
                    missingReqs.Add($"Requires {requiredEduStr} degree (Candidate has {applicant.HighestEducation ?? "None"})");
            }
            
            if (jobReqs.RequiredMajors.Any() && !string.IsNullOrEmpty(applicant.HighestEducation))
            {
                bool majorMatches = jobReqs.RequiredMajors.Any(m => applicant.HighestEducation.Contains(m, StringComparison.OrdinalIgnoreCase));
                if (!majorMatches)
                     missingReqs.Add($"Requires degree in: {string.Join(" or ", jobReqs.RequiredMajors)}");
            }

            foreach (var skill in jobReqs.MustHaveSkills)
            {
                if (!candidateSkills.Contains(skill) && !resumeText.Contains(skill.ToLower()))
                {
                    missingReqs.Add($"Missing required skill: {skill}");
                }
            }

            decimal totalScore = Math.Min(100, Math.Round(
                skillScore * (decimal)(SkillWeight / 100) +
                expScore * (decimal)(ExperienceWeight / 100) +
                eduScore * (decimal)(EducationWeight / 100) +
                kwScore * (decimal)(KeywordWeight / 100) +
                certScore * (decimal)(CertificationBonus / 100) +
                careerScore * (decimal)(CareerGrowthWeight / 100) +
                industryScore * (decimal)(IndustryWeight / 100) +
                semanticAlignment * (decimal)(SemanticWeight / 100), 2));

            string verdict = "Qualified";

            if (eduScore < 100 || expScore < 100)
            {
                totalScore = 0;
                verdict = eduScore < 100 ? "Rejected: Strict Degree Requirement Not Met" : "Rejected: Strict Experience Requirement Not Met";
                if (eduScore < 100 && expScore < 100) verdict = "Rejected: Strict Degree & Experience Requirements Not Met";
            }
            else if (missingReqs.Any())
            {
                verdict = "Missing " + missingReqs.First() + (missingReqs.Count > 1 ? $" (+{missingReqs.Count - 1} more)" : "");
                totalScore = Math.Min(totalScore * 0.4m, 45m); // Cap at 45% if missing other hard requirements (e.g. skills)
            }

            _scoreCache[cacheKey] = (DateTime.Now, totalScore);

            // Apply 10% priority boost for registered applicants (hidden backend logic)
            if (application.ApplicantID != null && !missingReqs.Any())
            {
                totalScore = Math.Min(100m, totalScore * 1.1m);
            }

            application.MatchScore = totalScore;
            application.MatchVerdict = verdict;
            application.MissingRequirements = string.Join(";", missingReqs);
            application.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            sw.Stop();
            _logger.LogDebug("Match score calculated for app {AppId} in {Ms}ms", applicationId, sw.ElapsedMilliseconds);

            return totalScore;
        }

        private decimal CalculateSkillScoreOptimized(string jobSkills, HashSet<string> candidateSkills, string resumeText)
        {
            if (string.IsNullOrWhiteSpace(jobSkills)) return 100;

            var required = jobSkills.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!required.Any()) return 100;

            int matched = 0;
            foreach (var req in required)
            {
                if (candidateSkills.Contains(req)) { matched++; continue; }

                foreach (var (key, syns) in _techSynonyms.Value)
                {
                    if (req.Equals(key, StringComparison.OrdinalIgnoreCase) || syns.Contains(req, StringComparer.OrdinalIgnoreCase))
                    {
                        if (candidateSkills.Contains(key) || syns.Any(s => candidateSkills.Contains(s, StringComparer.OrdinalIgnoreCase)))
                        {
                            matched++;
                            break;
                        }
                    }
                }
            }

            return (decimal)matched / required.Count * 100;
        }

        private decimal CalculateExperienceScore(int requiredYears, int candidateYears)
        {
            if (requiredYears <= 0) return 100;
            if (candidateYears >= requiredYears) return 100;
            return (decimal)candidateYears / requiredYears * 100;
        }

        private decimal CalculateEducationScoreInternal(string? requiredEdu, string? candidateEdu, JobRequirements? jobReq = null)
        {
            if (string.IsNullOrWhiteSpace(requiredEdu) && (jobReq == null || string.IsNullOrEmpty(jobReq.RequiredDegreeLevel))) return 100;
            if (string.IsNullOrWhiteSpace(candidateEdu)) return 50;

            var reqLvlStr = !string.IsNullOrEmpty(jobReq?.RequiredDegreeLevel) ? jobReq.RequiredDegreeLevel : requiredEdu;
            var reqLvl = GetEducationLevel(reqLvlStr!);
            var candLvl = GetEducationLevel(candidateEdu);
            
            decimal score = candLvl >= reqLvl ? 100 : (decimal)candLvl / (reqLvl > 0 ? reqLvl : 1) * 100;

            // Check Major if required
            if (jobReq != null && jobReq.RequiredMajors.Any())
            {
                bool majorMatches = jobReq.RequiredMajors.Any(m => candidateEdu.Contains(m, StringComparison.OrdinalIgnoreCase));
                if (!majorMatches) score = Math.Min(score, 40); // Penalty for wrong major even if level is high
            }

            return score;
        }

        private int GetEducationLevel(string edu)
        {
            foreach (var (key, value) in _educationLevelMap.Value.OrderByDescending(x => x.Value))
            {
                if (edu.Contains(key, StringComparison.OrdinalIgnoreCase)) return value;
            }
            return 0;
        }

        private decimal CalculateKeywordScore(string jobDescription, string resumeText)
        {
            if (string.IsNullOrWhiteSpace(jobDescription) || string.IsNullOrWhiteSpace(resumeText)) return 0;

            var jobTokens = jobDescription.ToLower().Split(new[] { ' ', ',', ';', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2).Distinct().ToHashSet();
            var resumeTokens = resumeText.Split(new[] { ' ', ',', ';', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2).Distinct().ToHashSet();

            if (!jobTokens.Any()) return 100;

            var intersection = jobTokens.Intersect(resumeTokens).Count();
            return (decimal)intersection / jobTokens.Count * 100;
        }

        private decimal CalculateCertificationScore(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText)) return 0;
            int count = _knownCertifications.Value.Count(c => resumeText.Contains(c, StringComparison.OrdinalIgnoreCase));
            return count >= 3 ? 100 : count * 33;
        }

        private decimal CalculateCareerGrowthScore(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText)) return 50;
            var score = 50;
            var growthTerms = new[] { "promoted", "lead", "senior", "principal", "manager", "director", "mentored", "achieved" };
            foreach (var term in growthTerms) { if (resumeText.Contains(term)) score += 5; }
            return Math.Min(score, 100);
        }

        private decimal CalculateIndustryMatchScore(string jobDescription, string resumeText)
        {
            if (string.IsNullOrWhiteSpace(jobDescription) || string.IsNullOrWhiteSpace(resumeText)) return 50;
            var text = (jobDescription + " " + resumeText).ToLower();
            var industries = new[] { "fintech", "healthcare", "ecommerce", "saas", "gaming", "edtech", "telecom", "automotive" };
            int matches = industries.Count(i => text.Contains(i));
            return Math.Min(50 + matches * 10, 100);
        }



        public async Task<List<Job>> GetRecommendedJobsForApplicantAsync(int applicantId)
        {
            var applicant = await _db.Applicants
                .AsNoTracking()
                .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId);

            if (applicant == null) return new List<Job>();

            var skills = applicant.ApplicantSkills.Select(s => s.Skill.SkillName.ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var jobs = await _db.Jobs
                .AsNoTracking()
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.Company)
                .Where(j => j.JobStatus == "Open" && j.ApplicationDeadline >= DateTime.Today)
                .OrderByDescending(j => j.CreatedDate)
                .Take(100)
                .ToListAsync();

            return jobs
                .OrderByDescending(j => CalculateJobMatchScore(j, skills))
                .Take(10)
                .ToList();
        }

        private decimal CalculateJobMatchScore(Job job, HashSet<string> skills)
        {
            var required = (job.RequiredSkills ?? "").ToLower().Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToList();
            if (!required.Any()) return 50;

            int matches = required.Count(r => skills.Contains(r));
            return (decimal)matches / required.Count * 100;
        }

        public decimal CalculateSkillScore(string? jobSkills, List<string> candidateSkills, string resumeText)
            => CalculateSkillScoreOptimized(jobSkills ?? "", new HashSet<string>(candidateSkills, StringComparer.OrdinalIgnoreCase), resumeText.ToLower());

        public decimal CalculateExperienceScore(int requiredYears, int candidateYears, string jobDescription = "")
            => CalculateExperienceScore(requiredYears, candidateYears);

        public decimal CalculateEducationScore(string? requiredEdu, string? candidateEdu)
            => CalculateEducationScoreInternal(requiredEdu, candidateEdu);

        public async Task<SkillsGapAnalysis> AnalyzeSkillsGapAsync(int applicationId)
        {
            var app = await _db.Applications
                .AsNoTracking()
                .Include(a => a.Applicant).ThenInclude(ap => ap!.ApplicantSkills).ThenInclude(s => s.Skill)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return new SkillsGapAnalysis();

            if (app.Job == null || app.Applicant == null) return new SkillsGapAnalysis();

            var required = (app.Job.RequiredSkills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower()).ToList();
            var candidate = app.Applicant.ApplicantSkills?.Select(s => s.Skill.SkillName.ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var matched = required.Where(r => candidate.Contains(r)).ToList();
            var missing = required.Except(matched).ToList();

            return new SkillsGapAnalysis
            {
                ApplicationId = applicationId,
                JobTitle = app.Job.JobTitle,
                CandidateName = app.Applicant.User?.FullName ?? "Unknown",
                RequiredSkills = required,
                MatchedSkills = matched,
                MissingSkills = missing,
                MatchPercentage = required.Count > 0 ? Math.Round((double)matched.Count / required.Count * 100, 1) : 0,
                GapSeverity = missing.Count switch { 0 => "None", 1 => "Low", 2 => "Medium", _ => "High" }
            };
        }

        public async Task<List<JobRecommendation>> GetJobRecommendationsAsync(int applicantId)
        {
            var applicant = await _db.Applicants
                .AsNoTracking()
                .Include(ap => ap.ApplicantSkills).ThenInclude(s => s.Skill)
                .Include(ap => ap.Applications).ThenInclude(a => a.Job)
                .FirstOrDefaultAsync(ap => ap.ApplicantID == applicantId);

            if (applicant == null) return new List<JobRecommendation>();

            var skills = applicant.ApplicantSkills.Select(s => s.Skill.SkillName.ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var appliedIds = applicant.Applications.Select(a => a.JobID).ToHashSet();

            var jobs = await _db.Jobs
                .AsNoTracking()
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.Company)
                .Where(j => j.JobStatus == "Open" && !appliedIds.Contains(j.JobID))
                .OrderByDescending(j => j.CreatedDate)
                .Take(100)
                .ToListAsync();

            return jobs.Select(job =>
            {
                var required = (job.RequiredSkills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToLower()).ToList();
                var matched = required.Where(r => skills.Contains(r)).ToList();
                var missing = required.Except(matched).ToList();

                return new JobRecommendation
                {
                    JobId = job.JobID,
                    JobTitle = job.JobTitle,
                    CompanyName = job.Company?.CompanyName ?? job.Recruiter?.CompanyName ?? "Unknown",
                    MatchScore = required.Count > 0 ? Math.Round((double)matched.Count / required.Count * 100, 1) : 0,
                    MatchedSkills = matched,
                    MissingSkills = missing,
                    Location = job.JobLocation ?? "Not specified",
                    JobType = job.JobType,
                    PostedDate = job.CreatedDate
                };
            }).OrderByDescending(r => r.MatchScore).Take(10).ToList();
        }

        public async Task<AIMatchResult> AnalyzeMatchAsync(string cvText, string jobText)
        {
            return await AnalyzeMatchViaGeminiAsync(cvText, jobText);
        }

        private async Task<AIMatchResult> AnalyzeMatchViaGeminiAsync(string cvText, string jobText)
        {
            var result = new AIMatchResult();
            if (string.IsNullOrWhiteSpace(cvText) || string.IsNullOrWhiteSpace(jobText)) return result;

            var prompt = $@"You are an expert ATS (Applicant Tracking System).
Compare the CV below against the Job Description and return a structured analysis.

Job Description:
{jobText}

CV / Resume:
{cvText}

Return ONLY a valid JSON object with exactly this schema. No markdown, no backticks:
{{
  ""MatchScore"": 0,
  ""MatchedSkills"": [],
  ""MissingSkills"": [],
  ""Summary"": """"
}}";

            try
            {
                var jsonText = await GenerateTextAsync(prompt, 0.2f, true);
                if (string.IsNullOrWhiteSpace(jsonText)) return result;

                var parsed = JsonSerializer.Deserialize<AIMatchResult>(
                    jsonText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed != null) result = parsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AnalyzeMatchAsync failed");
            }

            return result;
        }

        public static void ClearCache()
        {
            _scoreCache.Clear();
            _jobReqCache.Clear();
        }

        private async Task<JobRequirements> GetOrExtractJobRequirementsAsync(Job job)
        {
            if (_jobReqCache.TryGetValue(job.JobID, out var req)) return req;

            var result = new JobRequirements();
            if (string.IsNullOrWhiteSpace(job.JobDescription)) return result;

            var prompt = $@"You are an expert ATS. Analyze the following Job Description and extract the strict, absolute 'Must-Have' requirements. 
If a requirement is marked as 'preferred', 'bonus', or 'advantage', DO NOT include it here. 
For 'RequiredDegreeLevel', use one of: High School, Associate, Bachelor, Master, PhD. If none is strictly required, leave empty.
For 'RequiredMajors', list the exact fields of study required (e.g. ['Computer Science', 'Engineering']). If any major is fine, leave empty.
For 'MustHaveSkills', only list skills that are absolutely mandatory for the job.

Job Description:
{job.JobDescription}

Return ONLY a valid JSON object matching this schema. No markdown:
{{
  ""RequiredDegreeLevel"": """",
  ""RequiredMajors"": [],
  ""MinimumYearsOfExperience"": 0,
  ""MustHaveSkills"": []
}}";

            try
            {
                var jsonText = await GenerateTextAsync(prompt, 0.1f, true);
                if (!string.IsNullOrWhiteSpace(jsonText))
                {
                    var parsed = JsonSerializer.Deserialize<JobRequirements>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) result = parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract job requirements");
            }

            _jobReqCache[job.JobID] = result;
            return result;
        }

        public async Task<string> GenerateDeepAnalysisAsync(int applicationId)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Applicant).ThenInclude(ap => ap!.ApplicantSkills).ThenInclude(sk => sk.Skill)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null || app.Job == null || app.Applicant == null)
            {
                return "Error: Could not retrieve application data for deep analysis.";
            }

            var candidateSkills = string.Join(", ", app.Applicant.ApplicantSkills?.Select(s => s.Skill.SkillName) ?? Array.Empty<string>());
            var resumeText = app.Applicant.ProfileSummary ?? "";
            var jobText = app.Job.JobDescription ?? "";

            var prompt = $@"You are an expert, highly critical Senior Technical Recruiter and Academic Reviewer. 
Your task is to provide a brutally honest, deeply analytical 'Deep Analysis' for a candidate applying to a specific job.

Candidate Profile Summary:
{resumeText}
Candidate Skills: {candidateSkills}
Candidate Education: {app.Applicant.HighestEducation}
Candidate Experience: {app.Applicant.YearsOfExperience} years

Job Description:
{jobText}
Job Required Education: {app.Job.EducationLevel}
Job Minimum Experience: {app.Job.MinimumExperience} years

Structure your response EXACTLY using the following Markdown structure with emojis:

### 🔍 Feasibility Assessment
(Provide a highly critical, realistic probability of success. If they are missing hard requirements like a specific degree, CGPA, or experience level, explicitly state that this is a critical roadblock that usually results in immediate rejection.)

### ⚖️ Profile Alignment
(Provide a balanced view of where their profile bridges the gap and where it falls short.)
**Where They Excel:**
* [Point 1]
* [Point 2]

**Where They Fall Short / Lean Elsewhere:**
* [Point 1]
* [Point 2]

### 🚀 Strategic Recommendations
(Provide actionable, tailored advice based on their profile and this specific role. What exactly should they do next to improve their chances for this role or redirect their career?)
* [Recommendation 1]
* [Recommendation 2]

Ensure the tone is professional, direct, and incredibly insightful. Do not sugarcoat missing hard requirements. Format the output cleanly in Markdown.";

            try
            {
                var markdownResponse = await GenerateTextAsync(prompt, 0.4f, false);
                return string.IsNullOrWhiteSpace(markdownResponse) 
                    ? "An error occurred while generating the deep analysis." 
                    : markdownResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate deep analysis for Application {AppId}", applicationId);
                return "An error occurred while generating the deep analysis. Please try again later.";
            }
        }

        private static string StripMarkdownFences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim();
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) text = text[7..];
            else if (text.StartsWith("```")) text = text[3..];
            if (text.EndsWith("```")) text = text[..^3];
            return text.Trim();
        }

        private async Task<string> GenerateTextAsync(string prompt, float temperature, bool jsonMode)
        {
            var errors = new List<string>();

            var order = new List<string> { "OpenAI", "Gemini", "OpenRouter", "Local" };

            foreach (var provider in order)
            {
                try
                {
                    var result = await TryProviderAsync(provider, prompt, temperature, jsonMode);
                    if (!string.IsNullOrWhiteSpace(result)) return result;
                    errors.Add($"{provider}: returned empty response");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AI provider {Provider} failed: {Error}. Trying next fallback.", provider, ex.Message);
                    errors.Add($"{provider}: {ex.Message}");
                }
            }

            throw new Exception($"All AI providers failed. Details: {string.Join(" | ", errors)}");
        }

        private async Task<string> TryProviderAsync(string provider, string prompt, float temperature, bool jsonMode)
        {
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return LocalAIService.GenerateText(prompt, jsonMode);
            }

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey  = _config["AISettings:OpenAI:ApiKey"];
                var baseUrl = _config["AISettings:OpenAI:BaseUrl"] ?? "http://rkapi.com/v1";
                var model   = _config["AISettings:OpenAI:Model"] ?? "gpt-4.1-mini";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenAI ApiKey is missing.");

                // Ensure baseUrl ends with /chat/completions
                string endpoint = baseUrl.TrimEnd('/') + "/chat/completions";

                var requestBody = new
                {
                    model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    }),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenAI HTTP {(int)response.StatusCode}: {body[..Math.Min(400, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                var text = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return jsonMode ? StripMarkdownFences(text) : text;
            }

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("Gemini ApiKey is missing.");

                var client = new Client(apiKey: apiKey);
                var response = await client.Models.GenerateContentAsync(
                    model: _config["AISettings:Gemini:Model"] ?? "gemini-1.5-flash",
                    contents: prompt,
                    config: new GenerateContentConfig
                    {
                        Temperature = temperature,
                        ResponseMimeType = jsonMode ? "application/json" : "text/plain"
                    }
                );

                if (response?.Candidates == null || response.Candidates.Count == 0)
                    throw new Exception("Gemini returned no candidates.");

                var text = response.Candidates[0].Content?.Parts?[0]?.Text ?? "";
                return jsonMode ? StripMarkdownFences(text) : text;
            }
            else if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:OpenRouter:ApiKey"];
                var model = _config["AISettings:OpenRouter:Model"] ?? "google/gemma-3-27b-it:free";

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new Exception("OpenRouter ApiKey is missing.");

                string endpoint = "https://openrouter.ai/api/v1/chat/completions";

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = temperature,
                    response_format = jsonMode ? new { type = "json_object" } : null
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("HTTP-Referer", "http://localhost:5000"); // Required for OpenRouter rankings
                request.Headers.Add("X-Title", "AIBasedResumeScreeningSystem");
                
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenRouter HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                var text = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return jsonMode ? StripMarkdownFences(text) : text;
            }


            throw new Exception($"Unknown provider: {provider}");
        }
    }
}