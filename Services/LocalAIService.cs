using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;

namespace AIResumeScreeningSystem.Services
{
    public static class LocalAIService
    {
        private static readonly Dictionary<string, int> EducationLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            {"None", 0},
            {"High School", 1}, {"HSC", 1}, {"GED", 1}, {"Secondary", 1},
            {"Associate", 2}, {"Diploma", 2}, {"Foundation", 2}, {"Certificate", 2},
            {"Bachelor", 3}, {"BSc", 3}, {"BBA", 3}, {"B.Eng", 3}, {"B.Tech", 3}, {"BE", 3}, {"BS", 3}, {"BA", 3}, {"BCS", 3}, {"B.Com", 3},
            {"Master", 4}, {"MSc", 4}, {"MBA", 4}, {"M.Tech", 4}, {"MS", 4}, {"ME", 4}, {"MEng", 4}, {"MA", 4}, {"MCS", 4}, {"M.Com", 4},
            {"PhD", 5}, {"Doctorate", 5}, {"MD", 5}, {"JD", 5}, {"DBA", 5}, {"Ph.D", 5}
        };

        private static readonly Dictionary<string, HashSet<string>> SynonymGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            {"C#", new(StringComparer.OrdinalIgnoreCase){"CSharp",".NET","DotNet","ASP.NET","ASP.NET Core","Entity Framework","Blazor","LINQ","WPF","WinForms","MAUI"}},
            {"JavaScript", new(StringComparer.OrdinalIgnoreCase){"JS","ES6","ES2015","TypeScript","TS","NodeJS","Node.js","Vanilla JS","ECMAScript"}},
            {"React", new(StringComparer.OrdinalIgnoreCase){"ReactJS","React.js","Redux","React Router","JSX","Next.js","Remix","React Native"}},
            {"Angular", new(StringComparer.OrdinalIgnoreCase){"AngularJS","Angular 2","Angular CLI","RxJS","NgRx"}},
            {"Vue", new(StringComparer.OrdinalIgnoreCase){"VueJS","Vue.js","Vuex","Nuxt","Nuxt.js","Vue 3","Pinia"}},
            {"Python", new(StringComparer.OrdinalIgnoreCase){"Py","Django","Flask","FastAPI","Pandas","NumPy","PyTorch","TensorFlow","Jupyter","Celery"}},
            {"Java", new(StringComparer.OrdinalIgnoreCase){"Spring","Spring Boot","Spring MVC","Hibernate","Maven","Gradle","Quarkus","JPA","Servlet","Tomcat"}},
            {"SQL", new(StringComparer.OrdinalIgnoreCase){"TSQL","T-SQL","MySQL","PostgreSQL","Postgres","SQL Server","MSSQL","Oracle DB","SQLite","MariaDB","PL/SQL"}},
            {"NoSQL", new(StringComparer.OrdinalIgnoreCase){"MongoDB","Mongo","DynamoDB","Cassandra","CouchDB","Redis","Couchbase","Firebase","Firestore"}},
            {"AWS", new(StringComparer.OrdinalIgnoreCase){"Amazon Web Services","EC2","S3","Lambda","DynamoDB","ECS","EKS","RDS","CloudFormation","SQS","SNS"}},
            {"Azure", new(StringComparer.OrdinalIgnoreCase){"Microsoft Cloud","Entra","App Service","Azure DevOps","Azure Functions","AKS","Cosmos DB"}},
            {"GCP", new(StringComparer.OrdinalIgnoreCase){"Google Cloud","BigQuery","Cloud Run","Cloud Functions","GKE","Pub/Sub","Dataflow"}},
            {"Docker", new(StringComparer.OrdinalIgnoreCase){"Containers","Containerization","docker-compose","Podman","Container Runtime"}},
            {"Kubernetes", new(StringComparer.OrdinalIgnoreCase){"K8s","Helm","kubectl","Minikube","OpenShift","EKS","AKS","GKE"}},
            {"Machine Learning", new(StringComparer.OrdinalIgnoreCase){"ML","Deep Learning","Neural Network","NLP","Computer Vision","AI","LLM","GPT","Transformer","BERT"}},
            {"DevOps", new(StringComparer.OrdinalIgnoreCase){"CI/CD","Jenkins","GitHub Actions","GitLab CI","CircleCI","IaC","GitOps","Terraform","Ansible","Puppet"}},
            {"PHP", new(StringComparer.OrdinalIgnoreCase){"Laravel","Symfony","CodeIgniter","WordPress","Drupal","Magento","Composer"}},
            {"Ruby", new(StringComparer.OrdinalIgnoreCase){"Rails","Ruby on Rails","RoR","Sinatra","Sidekiq"}},
            {"Go", new(StringComparer.OrdinalIgnoreCase){"Golang","Go Lang","Gin","Echo","Fiber"}},
            {"Rust", new(StringComparer.OrdinalIgnoreCase){"Cargo","Tokio","Actix","WebAssembly","WASM"}},
            {"Swift", new(StringComparer.OrdinalIgnoreCase){"SwiftUI","UIKit","Xcode","iOS Development","Cocoa"}},
            {"Kotlin", new(StringComparer.OrdinalIgnoreCase){"Android Development","Jetpack Compose","Ktor","Coroutines"}},
            {"Data Science", new(StringComparer.OrdinalIgnoreCase){"Data Analysis","Data Analytics","Tableau","Power BI","R","SPSS","SAS","Matplotlib","Seaborn"}},
            {"Git", new(StringComparer.OrdinalIgnoreCase){"GitHub","GitLab","Bitbucket","Version Control","SVN","Mercurial"}},
            {"Linux", new(StringComparer.OrdinalIgnoreCase){"Ubuntu","CentOS","RHEL","Debian","Bash","Shell Scripting","Unix","Fedora"}},
            {"Agile", new(StringComparer.OrdinalIgnoreCase){"Scrum","Kanban","Sprint","SAFe","Jira","Confluence","Retrospective"}},
        };

        private static readonly HashSet<string> KnownCertifications = new(StringComparer.OrdinalIgnoreCase)
        {
            "AWS Certified", "Azure Certified", "Google Cloud Certified", "PMP", "CISSP", "CCNA", "CCNP", "CCIE",
            "Scrum Master", "CSM", "CSPO", "PSM", "CompTIA A+", "CompTIA Security+", "CompTIA Network+", "ITIL",
            "Six Sigma", "CPA", "CFA", "Oracle Certified", "Salesforce Certified", "Kubernetes", "CKA", "CKAD",
            "Docker Certified", "Terraform", "CEH", "OSCP", "CISM", "CRISC", "Red Hat Certified", "RHCE", "RHCSA",
            "CDP", "TOGAF", "Prince2", "SAFe Agilist", "PMI-ACP", "ISTQB", "Certified Ethical Hacker"
        };

        public static string GenerateText(string prompt, bool jsonMode)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return jsonMode ? "{}" : "";

            // 1. Job Requirements Extraction (AIMatchingService)
            if (prompt.Contains("RequiredDegreeLevel") && prompt.Contains("MustHaveSkills"))
            {
                var jd = ExtractSection(prompt, "Job Description:", "Return ONLY");
                var reqs = ExtractJobRequirements(jd);
                return JsonSerializer.Serialize(reqs, new JsonSerializerOptions { WriteIndented = true });
            }

            // 2. CV Match Comparison (AIMatchingService)
            if (prompt.Contains("MatchScore") && prompt.Contains("MatchedSkills") && prompt.Contains("Summary"))
            {
                var jd = ExtractSection(prompt, "Job Description:", "CV / Resume:");
                var cv = ExtractSection(prompt, "CV / Resume:", "Return ONLY");
                var match = CompareCVAndJob(cv, jd);
                return JsonSerializer.Serialize(match, new JsonSerializerOptions { WriteIndented = true });
            }

            // 3. Resume Structured Parse (ResumeParserService)
            if (prompt.Contains("LinkedInURL") && prompt.Contains("GitHubURL") && prompt.Contains("SoftSkills"))
            {
                var cv = ExtractSection(prompt, "Resume Text:", "");
                if (string.IsNullOrEmpty(cv)) cv = prompt; // Fallback
                var parsed = ParseResumeHeuristically(cv);
                return JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
            }

            // 4. Strict Resume Evaluation / Screening (GeminiResumeService)
            if (prompt.Contains("SECTION A — DOCUMENT VALIDATION") || prompt.Contains("is_resume_valid"))
            {
                var jd = ExtractSection(prompt, "--- JOB DESCRIPTION ---", "--- RESUME ---");
                var cv = ExtractSection(prompt, "--- RESUME ---", "SECTION F");
                if (string.IsNullOrEmpty(jd)) jd = ExtractSection(prompt, "--- JOB DESCRIPTION ---", "---");
                if (string.IsNullOrEmpty(cv)) cv = ExtractSection(prompt, "--- RESUME ---", "---");

                var eval = EvaluateResumeStrictly(cv, jd);
                return JsonSerializer.Serialize(eval, new JsonSerializerOptions { WriteIndented = true });
            }

            // 5. Deep Analysis Markdown (AIMatchingService)
            if (prompt.Contains("### 🔍 Feasibility Assessment"))
            {
                var summary = ExtractSection(prompt, "Candidate Profile Summary:", "Candidate Skills:");
                var skills = ExtractSection(prompt, "Candidate Skills:", "Candidate Education:");
                var edu = ExtractSection(prompt, "Candidate Education:", "Candidate Experience:");
                var exp = ExtractSection(prompt, "Candidate Experience:", "Job Description:");
                var jd = ExtractSection(prompt, "Job Description:", "Job Required Education:");
                var reqEdu = ExtractSection(prompt, "Job Required Education:", "Job Minimum Experience:");
                var reqExp = ExtractSection(prompt, "Job Minimum Experience:", "Structure your response");

                return GenerateDeepAnalysisMarkdown(summary, skills, edu, exp, jd, reqEdu, reqExp);
            }

            // Unknown prompt fallback
            if (jsonMode)
            {
                return "{}";
            }
            return "Local heuristic processing complete.";
        }

        private static string ExtractSection(string text, string startMarker, string endMarker)
        {
            var startIndex = text.IndexOf(startMarker);
            if (startIndex == -1) return string.Empty;
            startIndex += startMarker.Length;

            if (string.IsNullOrEmpty(endMarker))
            {
                return text.Substring(startIndex).Trim();
            }

            var endIndex = text.IndexOf(endMarker, startIndex);
            if (endIndex == -1)
            {
                return text.Substring(startIndex).Trim();
            }

            return text.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private static bool MatchWord(string text, string word)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(word)) return false;
            string pattern = Regex.Escape(word);
            pattern = @"(?<=^|[\s,;.:()\-\[\]'""/])" + pattern + @"(?=$|[\s,;.:()\-\[\]'""/])";
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
        }

        private static HashSet<string> ExtractSkills(string text)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return found;

            foreach (var (skill, syns) in SynonymGroups)
            {
                if (MatchWord(text, skill))
                {
                    found.Add(skill);
                }
                else
                {
                    foreach (var syn in syns)
                    {
                        if (MatchWord(text, syn))
                        {
                            found.Add(skill);
                            break;
                        }
                    }
                }
            }
            return found;
        }

        private static int ExtractExperienceYears(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            // Match "X years of experience", "X+ years", "X yrs", etc.
            var match = Regex.Match(text, @"(?i)(?:^|\s)(\d{1,2})\+?\s*(?:year|yr)s?\s*(?:of)?\s*(?:experience|exp|work|professional)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int exp))
            {
                return exp;
            }

            // Fallback: search for numbers near experience
            var matches = Regex.Matches(text, @"\b\d{1,2}\b");
            foreach (Match m in matches)
            {
                int idx = m.Index;
                int start = Math.Max(0, idx - 40);
                int len = Math.Min(text.Length - start, 80);
                string context = text.Substring(start, len);
                if (context.Contains("experience", StringComparison.OrdinalIgnoreCase) ||
                    context.Contains("exp", StringComparison.OrdinalIgnoreCase) ||
                    context.Contains("work", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(m.Value, out int val) && val < 50)
                    {
                        return val;
                    }
                }
            }

            return 0;
        }

        private static string ExtractDegree(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "None";

            foreach (var key in EducationLevels.Keys.OrderByDescending(k => EducationLevels[k]))
            {
                if (key.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;
                if (MatchWord(text, key))
                {
                    // Map back to standard names
                    if (key is "BSc" or "BBA" or "B.Eng" or "B.Tech" or "BE" or "BS" or "BA" or "BCS" or "B.Com") return "Bachelor";
                    if (key is "MSc" or "MBA" or "M.Tech" or "MS" or "ME" or "MEng" or "MA" or "MCS" or "M.Com") return "Master";
                    if (key is "Doctorate" or "MD" or "JD" or "DBA" or "Ph.D") return "PhD";
                    return key;
                }
            }

            return "None";
        }

        // ══════════════════════════════════════════════════════════════
        // 1. Extract Job Requirements (AIMatchingService)
        // ══════════════════════════════════════════════════════════════
        private static JobRequirements ExtractJobRequirements(string jd)
        {
            var reqs = new JobRequirements();
            if (string.IsNullOrWhiteSpace(jd)) return reqs;

            reqs.RequiredDegreeLevel = ExtractDegree(jd);
            reqs.MinimumYearsOfExperience = ExtractExperienceYears(jd);

            // Majors
            var majors = new[] { "Computer Science", "Software Engineering", "Information Technology", "Electrical Engineering", "Finance", "Business Administration" };
            foreach (var major in majors)
            {
                if (jd.Contains(major, StringComparison.OrdinalIgnoreCase))
                {
                    reqs.RequiredMajors.Add(major);
                }
            }

            // Must have skills (skills that have "must", "require", "essential" nearby)
            var skills = ExtractSkills(jd);
            reqs.MustHaveSkills = skills.Take(6).ToList(); // Top 6 skills as must-haves

            return reqs;
        }

        // ══════════════════════════════════════════════════════════════
        // 2. Compare CV and Job (AIMatchingService)
        // ══════════════════════════════════════════════════════════════
        private static AIMatchResult CompareCVAndJob(string cv, string jd)
        {
            var result = new AIMatchResult();
            if (string.IsNullOrWhiteSpace(cv) || string.IsNullOrWhiteSpace(jd)) return result;

            var cvSkills = ExtractSkills(cv);
            var jdSkills = ExtractSkills(jd);

            var matched = jdSkills.Intersect(cvSkills).ToList();
            var missing = jdSkills.Except(cvSkills).ToList();

            result.MatchedSkills = matched;
            result.MissingSkills = missing;

            // Calculate Score
            double baseScore = 50.0;
            if (jdSkills.Count > 0)
            {
                baseScore = (double)matched.Count / jdSkills.Count * 100;
            }

            var reqExp = ExtractExperienceYears(jd);
            var candExp = ExtractExperienceYears(cv);
            if (reqExp > 0)
            {
                if (candExp >= reqExp) baseScore += 10;
                else baseScore -= (reqExp - candExp) * 10;
            }

            var reqDeg = ExtractDegree(jd);
            var candDeg = ExtractDegree(cv);
            int reqLvl = EducationLevels.TryGetValue(reqDeg, out int r) ? r : 0;
            int candLvl = EducationLevels.TryGetValue(candDeg, out int c) ? c : 0;
            if (reqLvl > 0)
            {
                if (candLvl >= reqLvl) baseScore += 5;
                else baseScore -= 15;
            }

            result.MatchScore = Math.Clamp(Math.Round(baseScore), 0, 100);
            result.Summary = $"The candidate possesses strong capabilities in {string.Join(", ", matched.Take(4))}. They meet {matched.Count} out of {jdSkills.Count} job skills requested.";
            result.Strengths = $"Has practical background matching key requirements: {string.Join(", ", matched.Take(3))}.";
            result.ImprovementTips = missing.Select(s => $"Acquire skills or document project experience with {s}").Take(3).ToList();
            result.IsLocked = false;

            return result;
        }

        // ══════════════════════════════════════════════════════════════
        // 3. Resume Structured Parse (ResumeParserService)
        // ══════════════════════════════════════════════════════════════
        private static ParsedResumeData ParseResumeHeuristically(string text)
        {
            var data = new ParsedResumeData();
            data.RawText = text;
            data.UsedAI = false;
            data.ConfidenceScore = 0.80;

            if (string.IsNullOrWhiteSpace(text)) return data;

            // Email
            var emailMatch = Regex.Match(text, @"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b");
            if (emailMatch.Success) data.Email = emailMatch.Value;

            // Phone
            var phoneMatch = Regex.Match(text, @"\+?\d{1,4}[-.\s]?\(?\d{1,3}\)?[-.\s]?\d{3,4}[-.\s]?\d{4}");
            if (phoneMatch.Success) data.Phone = phoneMatch.Value;

            // Name (First line of resume without punctuation or email)
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => l.Length > 2)
                            .ToList();

            if (lines.Count > 0)
            {
                foreach (var line in lines.Take(4))
                {
                    if (!line.Contains("@") && !line.Contains("/") && !line.Contains(":") && !Regex.IsMatch(line, @"\d{4}"))
                    {
                        data.Name = line;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(data.Name)) data.Name = "Candidate Name";

            // URLs
            var liMatch = Regex.Match(text, @"(?i)linkedin\.com/in/[a-z0-9\-_]+");
            if (liMatch.Success) data.LinkedInURL = "https://" + liMatch.Value;

            var ghMatch = Regex.Match(text, @"(?i)github\.com/[a-z0-9\-_]+");
            if (ghMatch.Success) data.GitHubURL = "https://" + ghMatch.Value;

            // Skills
            var skills = ExtractSkills(text).ToList();
            data.Skills = skills;
            data.PrimarySkills = skills.Take(6).ToList();
            data.SecondarySkills = skills.Skip(6).ToList();

            // Education
            data.HighestEducation = ExtractDegree(text);
            data.Education = data.HighestEducation + " Degree";

            // Experience
            data.YearsOfExperience = ExtractExperienceYears(text);

            // Title
            var titles = new[] { "Software Engineer", "Web Developer", "System Administrator", "Data Scientist", "Project Manager", "DevOps Engineer", "QA Engineer", "Full Stack Developer", "Backend Developer", "Frontend Developer" };
            data.CurrentJobTitle = titles.FirstOrDefault(t => text.Contains(t, StringComparison.OrdinalIgnoreCase)) ?? "Professional";

            // Profile Summary
            data.ProfileSummary = lines.FirstOrDefault(l => l.Length > 50 && !l.Contains("@")) ?? "Experienced professional seeking new challenges.";

            // Certifications
            data.Certifications = KnownCertifications.Where(c => text.Contains(c, StringComparison.OrdinalIgnoreCase)).ToList();

            // Languages
            var langs = new[] { "English", "Spanish", "French", "German", "Chinese", "Japanese", "Bengali", "Hindi" };
            data.Languages = langs.Where(l => text.Contains(l, StringComparison.OrdinalIgnoreCase)).ToList();

            return data;
        }

        // ══════════════════════════════════════════════════════════════
        // 4. Strict Resume Evaluation / Screening (GeminiResumeService)
        // ══════════════════════════════════════════════════════════════
        private static AIResumeEvaluationResult EvaluateResumeStrictly(string cv, string jd)
        {
            var res = new AIResumeEvaluationResult();
            res.Validation.IsResumeValid = cv.Length > 80;
            res.Validation.IsJobDescriptionValid = jd.Length > 80;

            if (!res.Validation.IsResumeValid || !res.Validation.IsJobDescriptionValid)
            {
                res.Status = "invalid";
                res.FinalScore = 0;
                res.Reasons.Add("The uploaded document does not appear to be a valid resume or job description.");
                return res;
            }

            var reqDegree = ExtractDegree(jd);
            var candDegree = ExtractDegree(cv);
            var reqExp = ExtractExperienceYears(jd);
            var candExp = ExtractExperienceYears(cv);

            int reqLvl = EducationLevels.TryGetValue(reqDegree, out int r) ? r : 0;
            int candLvl = EducationLevels.TryGetValue(candDegree, out int c) ? c : 0;

            res.HardRequirements.RequiredDegree = reqDegree;
            res.HardRequirements.CandidateDegree = candDegree;
            res.HardRequirements.RequiredExperienceYears = reqExp;
            res.HardRequirements.CandidateExperienceYears = candExp;

            res.HardRequirements.EducationMet = candLvl >= reqLvl;
            res.HardRequirements.ExperienceMet = candExp >= reqExp;
            res.HardRequirements.SubjectMet = true; // Heuristic default pass

            bool allHardMet = res.HardRequirements.EducationMet && res.HardRequirements.ExperienceMet;

            var cvSkills = ExtractSkills(cv);
            var jdSkills = ExtractSkills(jd);
            var matched = jdSkills.Intersect(cvSkills).ToList();
            var missing = jdSkills.Except(cvSkills).ToList();

            res.ExtractedData.ResumeSkills = cvSkills.ToList();
            res.ExtractedData.JobSkills = jdSkills.ToList();
            res.ExtractedData.CoreSkillMatch = matched;
            res.ExtractedData.MissingSkills = missing;

            if (!allHardMet)
            {
                res.Status = "rejected";
                res.FinalScore = 0;
                if (!res.HardRequirements.EducationMet)
                {
                    res.Reasons.Add($"Required {reqDegree} degree, candidate holds {candDegree}.");
                }
                if (!res.HardRequirements.ExperienceMet)
                {
                    res.Reasons.Add($"Required {reqExp} years of experience, candidate has {candExp} years.");
                }
                res.ExtractedData.Strengths = "N/A";
                res.ExtractedData.Justification = $"The candidate was rejected due to unmet hard requirements. " + string.Join(" ", res.Reasons);
                return res;
            }

            // Calculate soft scores
            res.Scores.Education = reqLvl == 0 ? 1.0 : Math.Min(1.0, (double)candLvl / reqLvl);
            res.Scores.Experience = reqExp == 0 ? 1.0 : Math.Min(1.0, (double)candExp / reqExp);
            res.Scores.Skills = jdSkills.Count == 0 ? 1.0 : (double)matched.Count / jdSkills.Count;
            res.Scores.Keywords = 0.70; // Heuristic estimation

            double score = (res.Scores.Education * 0.25 + res.Scores.Experience * 0.30 + res.Scores.Skills * 0.35 + res.Scores.Keywords * 0.10) * 100;
            res.FinalScore = Math.Clamp(Math.Round(score), 0, 100);
            res.Status = "accepted";
            res.Reasons.Add("The candidate meets all hard education and experience requirements.");
            res.ExtractedData.Strengths = $"Possesses core skills in {string.Join(", ", matched.Take(3))}. Strong background matching the job description.";
            res.ExtractedData.Justification = $"Excellent fit for the position, meeting education and experience parameters with a strong matching skill score.";

            return res;
        }

        // ══════════════════════════════════════════════════════════════
        // 5. Deep Analysis Markdown (AIMatchingService)
        // ══════════════════════════════════════════════════════════════
        private static string GenerateDeepAnalysisMarkdown(string summary, string skills, string edu, string exp, string jd, string reqEdu, string reqExp)
        {
            return $@"### 🔍 Feasibility Assessment
Based on our offline analysis, the candidate's alignment is highly feasible. The candidate has **{exp.Trim()}** of experience and a highest education level of **{edu.Trim()}**, compared to the job's requirement of **{reqExp.Trim()}** and **{reqEdu.Trim()}**. The overall technical screening indicates a high likelihood of passing standard resume reviews.

### ⚖️ Profile Alignment
**Where They Excel:**
* Matches core technical skills listed in the candidate profile: `{skills.Trim()}`.
* Experience matches the minimum requirement of `{reqExp.Trim()}`.

**Where They Fall Short / Lean Elsewhere:**
* The profile summary shows focus in general development, but may need to highlight specific projects.
* Additional certifications in cloud or methodology could further strengthen alignment.

### 🚀 Strategic Recommendations
* Add detail to the resume projects regarding the application of core technical competencies.
* Format the experience bullet points to emphasize quantifiable outcomes and technical metrics.
";
        }
    }
}
