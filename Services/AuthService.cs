using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AIResumeScreeningSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IResumeParserService _parser;
        private readonly IEmailVerificationService _verification;

        public AuthService(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env, IResumeParserService parser, IEmailVerificationService verification)
        {
            _db = db;
            _config = config;
            _env = env;
            _parser = parser;
            _verification = verification;
        }

        public async Task<(bool success, string message, User? user)> LoginAsync(LoginDTO dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.AccountStatus == "Active");

            if (user == null)
                return (false, "Invalid email or password.", null);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return (false, "Invalid email or password.", null);

            user.LastLoginDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;

            if (user.IsFirstLogin)
            {
                user.IsFirstLogin = false;
            }

            await _db.SaveChangesAsync();

            return (true, "Login successful.", user);
        }

        public async Task<(bool success, string message, User? user)> JobSeekerLoginAsync(LoginDTO dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.AccountStatus == "Active");

            if (user == null)
                return (false, "Invalid email or password.", null);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return (false, "Invalid email or password.", null);

            if (user.Role != "Applicant")
                return (false, "This account is not a Job Seeker account. Please use the Recruiter login.", null);

            user.LastLoginDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;

            if (user.IsFirstLogin)
            {
                user.IsFirstLogin = false;
            }

            // Track applicant LastLoginDate for activity-based ranking boost
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == user.UserID);
            if (applicant != null)
            {
                applicant.LastLoginDate = DateTime.Now;
                applicant.LastActivityDate = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return (true, "Login successful.", user);
        }

        public async Task<(bool success, string message, User? user)> RecruiterLoginAsync(LoginDTO dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.AccountStatus == "Active");

            if (user == null)
                return (false, "Invalid email or password.", null);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return (false, "Invalid email or password.", null);

            if (user.Role != "Recruiter")
                return (false, "This account is not a Recruiter account. Please use the Job Seeker login.", null);

            var recruiter = await _db.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserID == user.UserID);

            if (recruiter == null || recruiter.CompanyID == null)
                return (false, "Your recruiter profile is not fully set up. Please contact support.", null);

            user.LastLoginDate = DateTime.Now;
            user.UpdatedDate = DateTime.Now;

            if (user.IsFirstLogin)
            {
                user.IsFirstLogin = false;
            }

            await _db.SaveChangesAsync();

            return (true, "Login successful.", user);
        }

        public async Task<(bool success, string message)> RegisterAsync(RegisterDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                return (false, "An account with this email already exists.");

            if (await _db.Users.AnyAsync(u => u.FullName.ToLower() == dto.FullName.ToLower()))
                return (false, "This user name is already taken. Please choose another.");

            if (!string.IsNullOrEmpty(dto.PhoneNumber) && await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                return (false, "An account with this phone number already exists.");

            var role = !string.IsNullOrEmpty(dto.Role) ? dto.Role : "Applicant";

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                Role = role,
                AccountStatus = "Active",
                Credits = 0,
                EmailVerified = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (role == "Applicant")
            {
                // No immediate credits - will be granted on email verification
                var applicant = new Applicant
                {
                    UserID = user.UserID,
                    ProfileCompletionPercent = 20
                };
                _db.Applicants.Add(applicant);
                await _db.SaveChangesAsync();
            }

            await _db.SaveChangesAsync();
            return (true, "Registration successful. Please sign in.");
        }

        public async Task<(bool success, string message, User? user)> JobSeekerRegisterAsync(JobSeekerRegisterDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                return (false, "An account with this email already exists.", null);

            if (await _db.Users.AnyAsync(u => u.FullName.ToLower() == dto.FullName.ToLower()))
                return (false, "This user name is already taken. Please choose another.", null);

            if (await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                return (false, "An account with this phone number already exists.", null);

            // External Email Verification API
            if (!await _verification.IsValidEmailAsync(dto.Email))
                return (false, "The email address provided appears to be invalid or disposable. Please use a professional email.", null);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                Role = "Applicant",
                AccountStatus = "Active",
                Credits = 0,
                EmailVerified = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var applicant = new Applicant
            {
                UserID = user.UserID,
                ProfileCompletionPercent = 20
            };

            // Save applicant to generate ApplicantID before mapping any skills
            _db.Applicants.Add(applicant);
            await _db.SaveChangesAsync();

            // ── Handle optional CV upload during registration ──
            if (dto.ResumeFile != null && dto.ResumeFile.Length > 0)
            {
                var allowed = new[] { ".pdf", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(dto.ResumeFile.FileName).ToLowerInvariant();
                if (allowed.Contains(ext))
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "resumes");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var path = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await dto.ResumeFile.CopyToAsync(stream);
                    }

                    applicant.ResumeFilePath = $"/uploads/resumes/{fileName}";

                    // ── Handle Parsed/Reviewed Data from DTO ──
                    if (!string.IsNullOrEmpty(dto.HighestEducation)) applicant.HighestEducation = dto.HighestEducation;
                    if (dto.YearsOfExperience.HasValue) applicant.YearsOfExperience = dto.YearsOfExperience.Value;
                    if (!string.IsNullOrEmpty(dto.CurrentJobTitle)) applicant.CurrentJobTitle = dto.CurrentJobTitle;
                    if (!string.IsNullOrEmpty(dto.LinkedInURL)) applicant.LinkedInURL = dto.LinkedInURL;
                    if (!string.IsNullOrEmpty(dto.GitHubURL)) applicant.PortfolioURL = dto.GitHubURL; // Map GitHub to Portfolio if needed or add GitHub field to Applicant
                    if (!string.IsNullOrEmpty(dto.ProfileSummary)) applicant.ProfileSummary = dto.ProfileSummary;

                    // Handle Skills from DTO (manual review)
                    if (!string.IsNullOrEmpty(dto.ExtractedSkills))
                    {
                        var skills = dto.ExtractedSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct()
                            .Take(20);

                        foreach (var skillName in skills)
                        {
                            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName)
                                ?? new Skill { SkillName = skillName };
                            
                            if (skill.SkillID == 0) _db.Skills.Add(skill);
                            await _db.SaveChangesAsync();

                            if (!_db.ApplicantSkills.Any(asig => asig.ApplicantID == applicant.ApplicantID && asig.SkillID == skill.SkillID))
                            {
                                _db.ApplicantSkills.Add(new ApplicantSkill
                                {
                                    ApplicantID = applicant.ApplicantID,
                                    SkillID = skill.SkillID
                                });
                            }
                        }
                    }
                    else 
                    {
                        // Fallback to auto-parse if DTO fields are empty (safety)
                        try
                        {
                            var parsed = await _parser.ParseResumeAsync(applicant.ResumeFilePath);
                            if (parsed.YearsOfExperience.HasValue) 
                                applicant.YearsOfExperience = parsed.YearsOfExperience.Value;
                            if (!string.IsNullOrEmpty(parsed.Education)) 
                                applicant.HighestEducation = parsed.HighestEducation ?? parsed.Education;
                            
                            if (!string.IsNullOrEmpty(parsed.Phone) && user != null)
                                user.PhoneNumber = parsed.Phone;

                            if (!string.IsNullOrEmpty(parsed.CurrentJobTitle))
                                applicant.CurrentJobTitle = parsed.CurrentJobTitle;

                            if (!string.IsNullOrEmpty(parsed.LinkedInURL))
                                applicant.LinkedInURL = parsed.LinkedInURL;

                            if (!string.IsNullOrEmpty(parsed.PortfolioURL))
                                applicant.PortfolioURL = parsed.PortfolioURL;
                            else if (!string.IsNullOrEmpty(parsed.GitHubURL))
                                applicant.PortfolioURL = parsed.GitHubURL;

                            if (!string.IsNullOrEmpty(parsed.Address))
                            {
                                applicant.Address = parsed.Address;
                                applicant.CurrentLocation = parsed.Address;
                            }

                            if (!string.IsNullOrEmpty(parsed.ProfileSummary))
                                applicant.ProfileSummary = parsed.ProfileSummary;

                            if (parsed.Certifications.Any())
                                applicant.Certifications = string.Join(", ", parsed.Certifications);
                            
                            if (parsed.Languages.Any())
                                applicant.Languages = string.Join(", ", parsed.Languages);
                            
                            if (parsed.Projects.Any())
                                applicant.Projects = string.Join("\n\n", parsed.Projects);
                            
                            if (parsed.Achievements.Any())
                                applicant.Achievements = string.Join("\n\n", parsed.Achievements);

                            // Auto-add extracted skills
                            if (parsed.Skills.Any())
                            {
                                foreach (var skillName in parsed.Skills.Take(15))
                                {
                                    var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName.ToLower() == skillName.ToLower());
                                    if (skill == null)
                                    {
                                        skill = new Skill { SkillName = skillName, CreatedDate = DateTime.Now, SkillCategory = "AI Extracted" };
                                        _db.Skills.Add(skill);
                                        await _db.SaveChangesAsync();
                                    }

                                    if (!_db.ApplicantSkills.Any(asig => asig.ApplicantID == applicant.ApplicantID && asig.SkillID == skill.SkillID))
                                    {
                                        _db.ApplicantSkills.Add(new ApplicantSkill
                                        {
                                            ApplicantID = applicant.ApplicantID,
                                            SkillID = skill.SkillID,
                                            SkillLevel = "Intermediate"
                                        });
                                    }
                                }
                            }
                        }
                        catch { /* Ignore fallback errors */ }
                    }

                    applicant.ProfileCompletionPercent = CalculateRegistrationCompletion(applicant, null);
                }
            }

            // Update applicant changes (like parsed CV info)
            _db.Applicants.Update(applicant);
            await _db.SaveChangesAsync();

            return (true, "Job Seeker registration successful.", user);
        }

        public async Task<ParsedResumeData> PreviewParseResumeAsync(string filePath)
        {
            return await _parser.ParseResumeAsync(filePath);
        }

        /// <summary>
        /// Calculates profile completion percentage at registration time based on
        /// filled fields and parsed resume data.
        /// </summary>
        private static int CalculateRegistrationCompletion(Applicant applicant, ParsedResumeData? parsed)
        {
            int percent = 20; // Base: account created

            if (!string.IsNullOrEmpty(applicant.ResumeFilePath)) percent += 15;
            if (applicant.YearsOfExperience > 0) percent += 10;
            if (!string.IsNullOrEmpty(applicant.HighestEducation)) percent += 10;
            if (!string.IsNullOrEmpty(applicant.ProfileSummary)) percent += 10;
            if (parsed != null && parsed.Skills.Any()) percent += 10;
            if (parsed != null && parsed.Certifications.Any()) percent += 5;

            return Math.Min(percent, 85); // Cap at 85 — full 100 requires manual profile completion
        }

        public async Task<(bool success, string message, User? user)> RecruiterRegisterAsync(RecruiterRegisterDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                return (false, "An account with this email already exists.", null);

            if (await _db.Users.AnyAsync(u => u.FullName.ToLower() == dto.FullName.ToLower()))
                return (false, "An account with this username already exists. Please choose a different User name.", null);

            if (!string.IsNullOrEmpty(dto.PhoneNumber) && await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                return (false, "An account with this phone number already exists.", null);

            // External Email Verification API
            if (!await _verification.IsValidEmailAsync(dto.Email))
                return (false, "The email address provided appears to be invalid or disposable. Please use a professional email.", null);

            var company = new Company
            {
                CompanyName = dto.CompanyName,
                CompanyNameBangla = dto.CompanyNameBangla,
                OrganizationCategory = dto.OrganizationCategory,
                IndustryType = dto.IndustryType,
                CompanyWebsite = dto.CompanyWebsite,
                Tags = dto.Tags,
                Tagline = dto.Tagline,
                EstablishmentYear = dto.EstablishmentYear,
                CompanySize = dto.CompanySize ?? "1-10",
                OrganizationEmail = dto.OrganizationEmail,
                AboutOrganization = dto.AboutOrganization,
                Vision = dto.Vision,
                Mission = dto.Mission,
                Division = dto.Division,
                District = dto.District,
                Thana = dto.Thana,
                PostalCode = dto.PostalCode,
                FullAddress = dto.FullAddress,
                FullAddressBangla = dto.FullAddressBangla,
                MobileNumber = dto.OrganizationMobile,
                LandPhoneNumber = dto.LandPhoneNumber,
                CompanyStatus = "Active"
            };

            // Handle File Uploads
            if (dto.LogoFile != null) company.LogoPath = await SaveFileAsync(dto.LogoFile, "logos");
            if (dto.CoverPhotoFile != null) company.CoverPhotoPath = await SaveFileAsync(dto.CoverPhotoFile, "covers");
            if (dto.SignatureFile != null) company.SignaturePath = await SaveFileAsync(dto.SignatureFile, "signatures");

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                Role = "Recruiter",
                AccountStatus = "Active"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var recruiter = new Recruiter
            {
                UserID = user.UserID,
                CompanyID = company.CompanyID,
                Designation = dto.Designation ?? "Recruiter",
                RecruiterStatus = "Active"
            };
            _db.Recruiters.Add(recruiter);
            await _db.SaveChangesAsync();

            return (true, "Recruiter registration successful. Your account is pending approval.", user);
        }


        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{fileName}";
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["JwtSettings:SecretKey"] ?? "DefaultSecretKey32Characters!!"));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "480")),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int? GetCurrentUserId(HttpContext context)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : null;
        }

        public string? GetCurrentUserRole(HttpContext context)
            => context.User.FindFirst(ClaimTypes.Role)?.Value;
        public async Task<bool> CheckAvailabilityAsync(string type, string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            return type.ToLower() switch
            {
                "email" => !await _db.Users.AnyAsync(u => u.Email.ToLower() == value.ToLower()),
                "phone" => !await _db.Users.AnyAsync(u => u.PhoneNumber == value),
                "username" => !await _db.Users.AnyAsync(u => u.FullName.ToLower() == value.ToLower()),
                _ => true
            };
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return (false, "Current password is incorrect.");
            }

            // Hash and set new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedDate = DateTime.Now;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return (true, "Password successfully updated.");
        }
    }
}