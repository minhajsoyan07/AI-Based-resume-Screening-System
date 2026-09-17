using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    public class ResumeApiController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly IResumeParserService _resumeParser;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ResumeApiController> _logger;

        public ResumeApiController(
            ApplicationDbContext db,
            IResumeParserService resumeParser,
            IWebHostEnvironment env,
            ILogger<ResumeApiController> logger)
        {
            _db = db;
            _resumeParser = resumeParser;
            _env = env;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded" });

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { success = false, message = "Invalid file format. Allowed: PDF, DOCX, DOC, JPG, JPEG, PNG" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { success = false, message = "File size exceeds 5MB limit" });

            // ── PDF Page Count Limit Guard (5 pages max) ──
            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(file))
            {
                return BadRequest(new { success = false, message = "Invalid resume: Document exceeds the maximum limit of 5 pages." });
            }

            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(applicant.ResumeFilePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, applicant.ResumeFilePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            applicant.ResumeFilePath = $"/uploads/resumes/{fileName}";
            await _db.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    // Use the resume parser service for parsing
                    var parsedData = await _resumeParser.ParseResumeAsync(applicant.ResumeFilePath);
                    
                    applicant.ProfileSummary = parsedData.ProfileSummary;
                    applicant.HighestEducation = parsedData.HighestEducation;
                    applicant.CurrentLocation = parsedData.Address;
                    applicant.YearsOfExperience = parsedData.YearsOfExperience ?? 0;
                    if (parsedData.YearsOfExperience.HasValue)
                        applicant.YearsOfExperience = (int)parsedData.YearsOfExperience.Value;
                    applicant.ProfileCompletionPercent = (int)parsedData.ProfileCompletionScore;
                    applicant.CurrentJobTitle = parsedData.CurrentJobTitle;
                    await _db.SaveChangesAsync();

                    var existingSkills = await _db.ApplicantSkills
                        .Where(s => s.ApplicantID == applicant.ApplicantID)
                        .ToListAsync();
                    _db.ApplicantSkills.RemoveRange(existingSkills);

                    foreach (var skillName in parsedData.Skills.Take(20))
                    {
                        var skill = await _db.Skills.FirstOrDefaultAsync(s => 
                            s.SkillName.ToLower() == skillName.ToLower());

                        if (skill == null)
                        {
                            skill = new Skill { SkillName = skillName, SkillCategory = "Detected" };
                            _db.Skills.Add(skill);
                            await _db.SaveChangesAsync();
                        }

                        _db.ApplicantSkills.Add(new ApplicantSkill
                        {
                            ApplicantID = applicant.ApplicantID,
                            SkillID = skill.SkillID,
                            SkillLevel = "Intermediate"
                        });
                    }
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("CV parsed successfully for applicant {ApplicantId}", applicant.ApplicantID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing CV for applicant {ApplicantId}", applicant.ApplicantID);
                }
            });

            return Ok(new
            {
                success = true,
                message = "Resume uploaded successfully. Parsing in progress...",
                data = new
                {
                    filePath = applicant.ResumeFilePath,
                    status = "Parsing"
                }
            });
        }

        [HttpGet("my")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> GetMyResume()
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants
                .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                .FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    filePath = applicant.ResumeFilePath,
                    profileSummary = applicant.ProfileSummary,
                    highestEducation = applicant.HighestEducation,
                    yearsOfExperience = applicant.YearsOfExperience,
                    currentJobTitle = applicant.CurrentJobTitle,
                    profileCompletion = applicant.ProfileCompletionPercent,
                    skills = applicant.ApplicantSkills.Select(s => new
                    {
                        name = s.Skill.SkillName,
                        level = s.SkillLevel
                    })
                }
            });
        }

        [HttpGet("parse-status")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> GetParseStatus()
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            var isParsed = !string.IsNullOrEmpty(applicant.ProfileSummary) || 
                            !string.IsNullOrEmpty(applicant.HighestEducation) ||
                            applicant.YearsOfExperience > 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    status = isParsed ? "Completed" : "Pending",
                    profileCompletion = applicant.ProfileCompletionPercent
                }
            });
        }

        [HttpDelete("delete")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> DeleteResume()
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            // Delete the file if it exists
            if (!string.IsNullOrEmpty(applicant.ResumeFilePath))
            {
                var filePath = Path.Combine(_env.WebRootPath, applicant.ResumeFilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Reset resume-related fields
            applicant.ResumeFilePath = null;
            applicant.ProfileSummary = null;
            applicant.HighestEducation = null;
            applicant.CurrentLocation = null;
            applicant.Address = null;
            applicant.YearsOfExperience = 0;
            applicant.CurrentJobTitle = null;
            applicant.ProfileCompletionPercent = 0;

            // Remove associated skills
            var existingSkills = await _db.ApplicantSkills
                .Where(s => s.ApplicantID == applicant.ApplicantID)
                .ToListAsync();
            _db.ApplicantSkills.RemoveRange(existingSkills);

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Resume deleted successfully" });
        }
    }
}