using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using Microsoft.Extensions.Logging;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize]
    public class MatchingController : BaseController
    {
        private readonly IAIResumeService _aiResumeService;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchingController> _logger;

        public MatchingController(IAIResumeService aiResumeService, ApplicationDbContext db, IWebHostEnvironment env, IConfiguration configuration, ILogger<MatchingController> logger)
        {
            _aiResumeService = aiResumeService;
            _db = db;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _db.Users
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.UserID == CurrentUserID);
                
            if (user == null) return RedirectToAction("Logout", "Auth");

            ViewBag.Credits = user.Credits;
            ViewBag.HasSavedCv = !string.IsNullOrEmpty(user.Applicant?.ResumeFilePath);
            SetAiSettingsViewBag();
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(IFormFile? cvFile, string? cvText, IFormFile? circularFile, string? jobDescription, bool useSavedCv = false)
        {
            var user = await _db.Users.FindAsync(CurrentUserID);
            if (user == null) return Unauthorized();

            ViewBag.Credits = user.Credits;
            SetAiSettingsViewBag();

            // Credit check
            if (user.Credits < 2)
            {
                ViewBag.Error = "Insufficient credits. You need 2 credits for a match analysis.";
                TempData["Error"] = "Insufficient credits. You need 2 credits for a match analysis.";
                return View();
            }

            // 1. Resolve CV text
            string resolvedCvText = "";
            
            if (useSavedCv)
            {
                var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == user.UserID);
                if (applicant != null && !string.IsNullOrEmpty(applicant.ResumeFilePath))
                {
                    try
                    {
                        var fullPath = Path.Combine(_env.WebRootPath, applicant.ResumeFilePath.TrimStart('/'));
                        resolvedCvText = await _aiResumeService.ParseDocumentByPathAsync(fullPath);
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = $"Failed to read your saved CV: {ex.Message}";
                        TempData["Error"] = $"Failed to read your saved CV: {ex.Message}";
                        return View();
                    }
                }
                else
                {
                    ViewBag.Error = "No saved CV found in your profile.";
                    TempData["Error"] = "No saved CV found in your profile.";
                    return View();
                }
            }
            else if (cvFile != null && cvFile.Length > 0)
            {
                if (Path.GetExtension(cvFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(cvFile))
                {
                    ViewBag.Error = "Invalid resume: Document exceeds the maximum limit of 5 pages.";
                    TempData["Error"] = "Invalid resume: Document exceeds the maximum limit of 5 pages.";
                    return View();
                }

                try
                {
                    resolvedCvText = await _aiResumeService.ParseDocumentAsync(cvFile);
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Failed to parse CV file: {ex.Message}";
                    TempData["Error"] = $"Failed to parse CV file: {ex.Message}";
                    return View();
                }
            }
            else if (!string.IsNullOrWhiteSpace(cvText))
            {
                resolvedCvText = cvText.Trim();
            }

            // 2. Resolve Job Description (file takes priority over text paste)
            string resolvedJobText = "";
            if (circularFile != null && circularFile.Length > 0)
            {
                if (Path.GetExtension(circularFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(circularFile))
                {
                    ViewBag.Error = "Invalid job circular: Document exceeds the maximum limit of 5 pages.";
                    TempData["Error"] = "Invalid job circular: Document exceeds the maximum limit of 5 pages.";
                    return View();
                }

                try
                {
                    resolvedJobText = await _aiResumeService.ParseDocumentAsync(circularFile);
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Failed to parse job circular file: {ex.Message}";
                    TempData["Error"] = $"Failed to parse job circular file: {ex.Message}";
                    return View();
                }
            }
            else if (!string.IsNullOrWhiteSpace(jobDescription))
            {
                resolvedJobText = jobDescription.Trim();
            }

            // Validation
            if (string.IsNullOrWhiteSpace(resolvedCvText))
            {
                ViewBag.Error = "Please provide your CV — upload a file or paste text.";
                TempData["Error"] = "Please provide your CV — upload a file or paste text.";
                return View();
            }
            if (string.IsNullOrWhiteSpace(resolvedJobText))
            {
                ViewBag.Error = "Please provide the job circular — upload a file or paste text.";
                TempData["Error"] = "Please provide the job circular — upload a file or paste text.";
                return View();
            }

            // Deduct credits
            user.Credits -= 2;
            _db.CreditTransactions.Add(new CreditTransaction
            {
                UserID = user.UserID,
                CreditsAmount = -2,
                ActionType = "MATCH",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            try
            {
                // 3. AI Analysis via Gemini
                // Note: Connection is released after SaveChangesAsync above, 
                // but we perform the long-running AI call outside a transaction context.
                var result = await _aiResumeService.AnalyzeResumeAsync(resolvedCvText.Trim(), resolvedJobText.Trim());

                if (result != null)
                {
                    // If validation failed (e.g. not a valid resume/job description)
                    if (!result.IsValid)
                    {
                        var refundUser = await _db.Users.FindAsync(user.UserID);
                        if (refundUser != null)
                        {
                            refundUser.Credits += 2;
                            _db.CreditTransactions.Add(new CreditTransaction 
                            { 
                                UserID = refundUser.UserID, 
                                CreditsAmount = 2, 
                                ActionType = "REFUND", 
                                CreatedAt = DateTime.Now 
                            });
                            await _db.SaveChangesAsync();
                            ViewBag.Credits = refundUser.Credits;
                        }
                        ViewBag.Error = "Validation Failed: " + result.ValidationError;
                        TempData["Error"] = result.ValidationError;
                        return View();
                    }

                    // Detect service-error fallback (API unavailable) vs genuine rejection
                    bool isServiceError = result.Verdict != null &&
                        (result.Verdict.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("high traffic", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("provider issues", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("service disruption", StringComparison.OrdinalIgnoreCase) ||
                         result.Justification?.Contains("service disruptions", StringComparison.OrdinalIgnoreCase) == true ||
                         result.Strengths == "System unavailable.");

                    if (isServiceError)
                    {
                        // Refund credits — user shouldn't pay for a system error
                        var refundUser = await _db.Users.FindAsync(user.UserID);
                        if (refundUser != null)
                        {
                            refundUser.Credits += 2;
                            _db.CreditTransactions.Add(new CreditTransaction { UserID = refundUser.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                            await _db.SaveChangesAsync();
                            ViewBag.Credits = refundUser.Credits;
                        }
                        TempData["Error"] = "AI service is temporarily unavailable. Your credits have been refunded. Please try again shortly.";
                        return View();
                    }

                    ViewBag.Credits = user.Credits;
                    if (!string.IsNullOrEmpty(result.AIProvider))
                    {
                        ViewBag.AIProvider = result.AIProvider;
                        ViewBag.AIModel = result.AIModel;
                    }
                    else
                    {
                        SetAiSettingsViewBag();
                    }
                    ViewBag.Result = result;
                    ViewBag.HasResult = true;
                    ViewBag.Success = result.MatchScore > 0
                        ? "AI Analysis completed successfully!"
                        : "Analysis complete — the submission was rejected. See details below.";
                    TempData["Success"] = ViewBag.Success;
                    return View();
                }
                else
                {
                    // AI returned null — re-fetch user to ensure we have latest credit state for refund
                    var refundUser = await _db.Users.FindAsync(user.UserID);
                    if (refundUser != null)
                    {
                        refundUser.Credits += 2;
                        _db.CreditTransactions.Add(new CreditTransaction { UserID = refundUser.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                        await _db.SaveChangesAsync();
                        ViewBag.Credits = refundUser.Credits;
                    }
                    TempData["Error"] = "AI analysis failed. Your credits have been refunded.";
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "An error occurred during resume matching analysis.");
                }
                else
                {
                    Console.WriteLine($"[Error] Match analysis failed: {ex.Message}\n{ex.StackTrace}");
                }

                // Error occurred, re-fetch user for refund
                var refundUser = await _db.Users.FindAsync(user.UserID);
                if (refundUser != null)
                {
                    refundUser.Credits += 2;
                    _db.CreditTransactions.Add(new CreditTransaction { UserID = refundUser.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                    await _db.SaveChangesAsync();
                    ViewBag.Credits = refundUser.Credits;
                }
                TempData["Error"] = "An error occurred during analysis. Your credits have been refunded.";
            }

            return View();
        }

        private void SetAiSettingsViewBag()
        {
            var provider = _configuration["AISettings:Provider"] ?? "OpenAI";
            ViewBag.AIProvider = provider;
            ViewBag.AIModel = provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) 
                ? (_configuration["AISettings:OpenAI:Model"] ?? "gpt-5.5")
                : (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
                    ? (_configuration["AISettings:Gemini:Model"] ?? "gemini-2.0-flash")
                    : (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                        ? (_configuration["AISettings:OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free")
                        : "Local Heuristic Matcher"));
        }
    }
}
