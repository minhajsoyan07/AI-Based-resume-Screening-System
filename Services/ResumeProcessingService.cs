using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIResumeScreeningSystem.Services
{
    public class ResumeProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ResumeProcessingService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

        public ResumeProcessingService(IServiceProvider serviceProvider, ILogger<ResumeProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Resume Processing Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var pendingResumes = await db.Applicants
                        .Where(a => !string.IsNullOrEmpty(a.ResumeFilePath) && 
                               (string.IsNullOrEmpty(a.ProfileSummary) || a.ProfileCompletionPercent < 50))
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    if (pendingResumes.Any())
                    {
                        var parser = scope.ServiceProvider.GetRequiredService<IResumeParserService>();
                        var matcher = scope.ServiceProvider.GetRequiredService<IAIMatchingService>();

                        foreach (var applicant in pendingResumes)
                        {
                            try
                            {
                                _logger.LogInformation("Processing resume for applicant {ApplicantId}", applicant.ApplicantID);

                                var parsedData = await parser.ParseResumeAsync(applicant.ResumeFilePath!);

                                applicant.ProfileSummary = parsedData.ProfileSummary;
                                applicant.HighestEducation = parsedData.HighestEducation;
                                applicant.YearsOfExperience = parsedData.YearsOfExperience ?? 0;
                                applicant.ProfileCompletionPercent = (int)parsedData.ProfileCompletionScore;
                                applicant.CurrentJobTitle = parsedData.CurrentJobTitle;

                                await db.SaveChangesAsync();

                                var existingSkills = await db.ApplicantSkills
                                    .Where(s => s.ApplicantID == applicant.ApplicantID)
                                    .ToListAsync(stoppingToken);
                                db.ApplicantSkills.RemoveRange(existingSkills);

                                foreach (var skillName in parsedData.Skills.Take(20))
                                {
                                    var skill = await db.Skills.FirstOrDefaultAsync(s => 
                                        s.SkillName.ToLower() == skillName.ToLower(), stoppingToken);

                                    if (skill == null)
                                    {
                                        skill = new Models.Skill { SkillName = skillName, SkillCategory = "Detected" };
                                        db.Skills.Add(skill);
                                        await db.SaveChangesAsync(stoppingToken);
                                    }

                                    db.ApplicantSkills.Add(new Models.ApplicantSkill
                                    {
                                        ApplicantID = applicant.ApplicantID,
                                        SkillID = skill.SkillID,
                                        SkillLevel = "Intermediate"
                                    });
                                }

                                await db.SaveChangesAsync(stoppingToken);

                                var applications = await db.Applications
                                    .Where(a => a.ApplicantID == applicant.ApplicantID)
                                    .ToListAsync(stoppingToken);

                                foreach (var app in applications)
                                {
                                    try
                                    {
                                        await matcher.CalculateMatchScoreAsync(app.ApplicationID);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error calculating match score for application {AppId}", app.ApplicationID);
                                    }
                                }

                                _logger.LogInformation("Resume processed successfully for applicant {ApplicantId}", applicant.ApplicantID);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing resume for applicant {ApplicantId}", applicant.ApplicantID);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in resume processing service");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Resume Processing Service stopped");
        }
    }
}