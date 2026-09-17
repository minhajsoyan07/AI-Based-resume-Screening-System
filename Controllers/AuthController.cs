using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using AIResumeScreeningSystem.Constants;

using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [AllowAnonymous]
    public class AuthController : BaseController
    {
        private readonly IAuthService _auth;
        private readonly IEmailService _email;
        private readonly ApplicationDbContext _db;

        public AuthController(IAuthService auth, IEmailService email, ApplicationDbContext db)
        {
            _auth = auth;
            _email = email;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login() => RedirectToAction("Index", "Home");

        [HttpGet]
        public IActionResult ApplicantLogin(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();
            
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicantLogin(LoginDTO dto, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.JobSeekerLoginAsync(dto);
            if (!success) 
            { 
                ModelState.AddModelError("", message); 
                return View(dto); 
            }

            return await ProcessSignInAsync(user!, "Applicant", dto.RememberMe, returnUrl);
        }

        [HttpGet]
        public IActionResult RecruiterLogin() =>
            User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecruiterLogin(LoginDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.RecruiterLoginAsync(dto);
            if (!success) 
            { 
                ModelState.AddModelError("", message); 
                return View(dto); 
            }

            return await ProcessSignInAsync(user!, "Recruiter", dto.RememberMe);
        }

        [HttpGet]
        public IActionResult AdminLogin() =>
            User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.LoginAsync(dto);
            if (!success) { ModelState.AddModelError("", message); return View(dto); }

            if (user!.Role != "Admin")
            {
                ModelState.AddModelError("", "Access denied. Admin credentials required.");
                return View(dto);
            }

            return await ProcessSignInAsync(user, "Admin", dto.RememberMe);
        }

        [HttpGet]
        public IActionResult JobSeekerRegister()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role) ?? "Applicant");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> JobSeekerRegister(JobSeekerRegisterDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.JobSeekerRegisterAsync(dto);
            if (!success) { ModelState.AddModelError("", message); return View(dto); }

            // Email is verified via OTP before this form is submitted
            user!.EmailVerified = true;
            user.Credits = 2; // Award 2 free credits immediately
            
            _db.CreditTransactions.Add(new CreditTransaction
            {
                UserID = user.UserID,
                CreditsAmount = 2,
                ActionType = "REGISTRATION_BONUS",
                CreatedAt = DateTime.Now,
                AdminVerified = true
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Welcome to Janala! Your account is now active. You have received 2 complimentary credits to get started.";
            return RedirectToAction("ApplicantLogin", "Auth");
        }

        [HttpPost]
        public async Task<IActionResult> PreviewParse(IFormFile resumeFile)
        {
            if (resumeFile == null || resumeFile.Length == 0)
                return Json(new { success = false, message = "No file uploaded" });

            var allowed = new[] { ".pdf", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return Json(new { success = false, message = "Unsupported file format" });

            // ── PDF Page Count Limit Guard (5 pages max) ──
            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(resumeFile))
            {
                return Json(new { success = false, message = "Invalid resume: Document exceeds the maximum limit of 5 pages." });
            }

            // Temporary save for parsing
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "parsing");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await resumeFile.CopyToAsync(stream);
            }

            try
            {
                var relativePath = $"/temp/parsing/{fileName}";
                var parsed = await _auth.PreviewParseResumeAsync(relativePath);
                
                // Cleanup temp file after reading/parsing
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

                return Json(new { success = true, data = parsed });
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult RecruiterRegister()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role) ?? "Recruiter");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecruiterRegister(RecruiterRegisterDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.RecruiterRegisterAsync(dto);
            if (!success) { ModelState.AddModelError("", message); return View(dto); }

            // Email is verified via OTP before this form is submitted
            user!.EmailVerified = true;
            await _db.SaveChangesAsync();

            TempData["Info"] = "Your recruiter account has been created successfully. You may now sign in to access your Recruiter Portal.";
            return RedirectToAction("RecruiterLogin", "Auth");
        }

        [HttpGet]
        public IActionResult Register([FromQuery] string role = "Applicant")
        {
            if (role == "Recruiter")
                return RedirectToAction("RecruiterRegister", "Auth");
            return RedirectToAction("JobSeekerRegister", "Auth");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message) = await _auth.RegisterAsync(dto);
            if (!success) { ModelState.AddModelError("", message); return View(dto); }

            // General register (if used)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user != null)
            {
                var token = Guid.NewGuid().ToString("N");
                _db.EmailVerificationTokens.Add(new EmailVerificationToken
                {
                    UserID = user.UserID,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddHours(24)
                });
                await _db.SaveChangesAsync();
                await _email.SendVerificationEmailAsync(user.Email, user.FullName, token);
            }

            TempData["Success"] = "Registration successful! Please check your inbox to verify your email address and activate your Janala account.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Home");

            var verification = await _db.EmailVerificationTokens
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == token && !v.IsUsed && v.ExpiresAt > DateTime.Now);

            if (verification == null)
            {
                TempData["Error"] = "This verification link is invalid or has expired. Please register again or contact Janala support.";
                return RedirectToAction("Index", "Home");
            }

            var user = verification.User;
            if (user == null) return RedirectToAction("Index", "Home");

            user.EmailVerified = true;
            verification.IsUsed = true;

            // Award 2 free credits if Applicant
            if (user.Role == "Applicant" && user.Credits == 0)
            {
                user.Credits = 2;
                _db.CreditTransactions.Add(new CreditTransaction
                {
                    UserID = user.UserID,
                    CreditsAmount = 2,
                    ActionType = "REGISTRATION_BONUS",
                    CreatedAt = DateTime.Now,
                    AdminVerified = true
                });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Your email has been verified successfully. You have received 2 complimentary credits. Welcome to Janala!";
            
            if (user.Role == "Recruiter") return RedirectToAction("RecruiterLogin");
            if (user.Role == "Admin") return RedirectToAction("AdminLogin");
            return RedirectToAction("ApplicantLogin");
        }

        private async Task<IActionResult> ProcessSignInAsync(User user, string expectedRole, bool rememberMe = false, string? returnUrl = null)
        {
            // Write session first so layout renders correctly before cookie response
            SetSessionData(user.UserID, user.Role, user.FullName, user.Email);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name,           user.FullName),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role),
                // Extra claim so header can show credits without extra DB call on every page
                new Claim("Credits",                 user.Credits.ToString())
            };

            var claimsIdentity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties  = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                // Session cookie expires with browser; persistent cookie lasts 30 days
                ExpiresUtc   = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToDashboard(user.Role);
        }

        private async Task<IActionResult> ProcessAdminSignInAsync(LoginDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, message, user) = await _auth.LoginAsync(dto);
            if (!success) { ModelState.AddModelError("", message); return View(dto); }

            if (user!.Role != "Admin")
            {
                ModelState.AddModelError("", "Access denied. Admin credentials required.");
                return View(dto);
            }

            return await ProcessSignInAsync(user, "Admin", dto.RememberMe);
        }

        public async Task<IActionResult> Logout()
        {
            ClearSession();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.AccountStatus == "Active");
            if (user != null)
            {
                var token = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);
                
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                await _email.SendPasswordResetEmailAsync(user.Email, user.FullName, token);
            }

            TempData["Success"] = "If your email is registered in our system, you will receive a password reset link shortly. Please check your inbox and spam folder.";
            return RedirectToAction("ForgotPassword");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Reset token is missing.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.Now && u.AccountStatus == "Active");
            if (user == null)
            {
                TempData["Error"] = "This password reset link is invalid or has expired. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            var dto = new ResetPasswordDTO { Token = token };
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token && u.PasswordResetTokenExpiry > DateTime.Now && u.AccountStatus == "Active");
            if (user == null)
            {
                TempData["Error"] = "This password reset link is invalid or has expired. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedDate = DateTime.Now;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your password has been reset successfully. You may now sign in with your new password.";
            
            if (user.Role == "Recruiter") return RedirectToAction("RecruiterLogin");
            if (user.Role == "Admin") return RedirectToAction("AdminLogin");
            return RedirectToAction("ApplicantLogin");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(string type, string value)
        {
            var isAvailable = await _auth.CheckAvailabilityAsync(type, value);
            return Json(new { available = isAvailable });
        }

        private IActionResult RedirectToDashboard(string role = "")
        {
            if (string.IsNullOrEmpty(role))
                role = CurrentUserRole ?? "Applicant";

            return role.ToUpper() switch
            {
                "ADMIN" => RedirectToAction("Index", "Admin"),
                "RECRUITER" => RedirectToAction("Index", "Recruiter"),
                _ => RedirectToAction("Index", "Applicant")
            };
        }
    }
}