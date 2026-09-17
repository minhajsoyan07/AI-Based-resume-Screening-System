using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AIResumeScreeningSystem.Services
{
    public interface IAutomationService
    {
        Task ProcessApplicationAsync(int applicationId);
    }

    public class AutomationService : IAutomationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAIMatchingService _matchingService;
        private readonly IResumeParserService _parserService;
        private readonly ILogger<AutomationService> _logger;

        public AutomationService(
            ApplicationDbContext db, 
            IAIMatchingService matchingService, 
            IResumeParserService parserService,
            ILogger<AutomationService> logger)
        {
            _db = db;
            _matchingService = matchingService;
            _parserService = parserService;
            _logger = logger;
        }

        public async Task ProcessApplicationAsync(int applicationId)
        {
            _logger.LogInformation("Starting automated processing for application {AppId}", applicationId);

            var application = await _db.Applications
                .Include(a => a.Applicant)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application == null) return;

            try 
            {
                // 1. Initial Parsing if not already done (Only for registered applicants)
                if (application.Applicant != null && string.IsNullOrEmpty(application.Applicant.ProfileSummary))
                {
                    _logger.LogDebug("Auto-parsing resume for application {AppId}", applicationId);
                    var resumePath = application.Applicant.ResumeFilePath;
                    if (!string.IsNullOrEmpty(resumePath)) 
                    {
                        var parsed = await _parserService.ParseResumeAsync(resumePath);
                        if (parsed != null)
                        {
                            var applicant = application.Applicant;
                            if (parsed.YearsOfExperience.HasValue && parsed.YearsOfExperience > 0 && applicant.YearsOfExperience == 0)
                                applicant.YearsOfExperience = parsed.YearsOfExperience ?? 0;
                            if (!string.IsNullOrEmpty(parsed.Education) && string.IsNullOrEmpty(applicant.HighestEducation))
                                applicant.HighestEducation = parsed.Education;
                            if (string.IsNullOrEmpty(applicant.ProfileSummary) && !string.IsNullOrEmpty(parsed.RawText))
                                applicant.ProfileSummary = $"AI Extracted Summary: {parsed.Education} | {parsed.YearsOfExperience} years exp.";
                            
                            // Simple skill sync
                            if (parsed.Skills.Any())
                            {
                                foreach (var skillName in parsed.Skills.Take(10))
                                {
                                    var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName)
                                        ?? new Skill { SkillName = skillName };
                                    if (skill.SkillID == 0) _db.Skills.Add(skill);

                                    if (!await _db.ApplicantSkills.AnyAsync(askill => askill.ApplicantID == applicant.ApplicantID && askill.SkillID == skill.SkillID))
                                    {
                                        _db.ApplicantSkills.Add(new ApplicantSkill { ApplicantID = applicant.ApplicantID, SkillID = skill.SkillID });
                                    }
                                }
                            }
                        }
                    }
                }

                // 2. Automated Scoring
                _logger.LogDebug("Auto-calculating match score for application {AppId}", applicationId);
                var score = await _matchingService.CalculateMatchScoreAsync(applicationId);

                // 3. Automated Action (Shortlisting Threshold: 80%)
                if (score >= 80 && application.ApplicationStatus == "Applied")
                {
                    _logger.LogInformation("Auto-shortlisting candidate for application {AppId} with score {Score}", applicationId, score);
                    application.ApplicationStatus = "Shortlisted";
                    application.Notes = "Automatically shortlisted based on exceptional match candidate/role alignment.";
                }

                // 4. Save changes

                await _db.SaveChangesAsync();
                _logger.LogInformation("Completed automated processing for application {AppId}", applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automated processing of application {AppId}", applicationId);
            }
        }
    }
}
