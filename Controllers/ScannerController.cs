using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Constants;
using Microsoft.Extensions.Logging;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize]
    public class ScannerController : BaseController
    {
        private readonly IAIMatchingService _matchingService;
        private readonly IResumeParserService _parserService;
        private readonly IAIResumeService _aiResumeService;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScannerController> _logger;

        public ScannerController(
            IAIMatchingService matchingService,
            IResumeParserService parserService,
            IAIResumeService aiResumeService,
            ApplicationDbContext db,
            IConfiguration configuration,
            ILogger<ScannerController> logger)
        {
            _matchingService = matchingService;
            _parserService = parserService;
            _aiResumeService = aiResumeService;
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _db.Users.FindAsync(CurrentUserID);
            if (user == null) return RedirectToAction("Logout", "Auth");

            ViewBag.Credits = user.Credits;
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PerformScan(IFormFile resumeFile, string jobDescription)
        {
            var user = await _db.Users.FindAsync(CurrentUserID);
            if (user == null) return Unauthorized();

            if (user.Credits < 2)
            {
                TempData["ErrorMessage"] = "Insufficient credits. You need 2 credits for a scan.";
                return RedirectToAction("Dashboard");
            }

            if (resumeFile == null || resumeFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload a valid resume (PDF or DOCX).";
                return RedirectToAction("Dashboard");
            }

            // ── Server-side file type guard (defence against browser bypass) ──
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };
            var fileExt = Path.GetExtension(resumeFile.FileName);
            if (!allowedExtensions.Contains(fileExt))
            {
                TempData["ErrorMessage"] = $"Invalid file type \"{fileExt}\". Only PDF and DOCX files are accepted as resumes.";
                return RedirectToAction("Dashboard");
            }

            // ── File size guard (10 MB max) ──
            if (resumeFile.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File is too large. Maximum allowed size is 10 MB.";
                return RedirectToAction("Dashboard");
            }

            // ── PDF Page Count Limit Guard (5 pages max) ──
            if (fileExt.Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(resumeFile))
            {
                TempData["ErrorMessage"] = "Invalid resume: Document exceeds the maximum limit of 5 pages.";
                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrWhiteSpace(jobDescription) || jobDescription.Trim().Length < 50)
            {
                TempData["ErrorMessage"] = "Please paste a meaningful job description (at least 50 characters).";
                return RedirectToAction("Dashboard");
            }


            // Deduct credits
            user.Credits -= 2;
            _db.CreditTransactions.Add(new CreditTransaction
            {
                UserID = user.UserID,
                CreditsAmount = -2,
                ActionType = "SCAN",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            try
            {
                // 1. Parse the Document (Smart PDF + OCR + DOCX tables + text cleaning)
                string resumeText = await _aiResumeService.ParseDocumentAsync(resumeFile);

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    TempData["ErrorMessage"] = "Failed to extract text from the uploaded file.";
                    // Refund credits on technical failure
                    user.Credits += 2;
                    _db.CreditTransactions.Add(new CreditTransaction { UserID = user.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                    await _db.SaveChangesAsync();
                    return RedirectToAction("Dashboard");
                }

                // 2. Send to Gemini for Matching (enforced JSON output)
                var result = await _aiResumeService.AnalyzeResumeAsync(resumeText, jobDescription);

                if (result != null)
                {
                    // If validation failed (e.g. not a valid resume/job description)
                    if (!result.IsValid)
                    {
                        user.Credits += 2;
                        _db.CreditTransactions.Add(new CreditTransaction
                        {
                            UserID = user.UserID,
                            CreditsAmount = 2,
                            ActionType = "REFUND",
                            CreatedAt = DateTime.Now
                        });
                        await _db.SaveChangesAsync();

                        TempData["ErrorMessage"] = result.ValidationError;
                        return RedirectToAction("Dashboard");
                    }

                    // Auto-refund if result is a technical/service error (not a genuine rejection)
                    bool isServiceError = result.Verdict != null &&
                        (result.Verdict.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("currently unavailable", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("high traffic", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("provider issues", StringComparison.OrdinalIgnoreCase) ||
                         result.Verdict.Contains("service disruption", StringComparison.OrdinalIgnoreCase) ||
                         result.Justification?.Contains("service disruptions", StringComparison.OrdinalIgnoreCase) == true ||
                         result.Strengths == "System unavailable.");

                    if (isServiceError)
                    {
                        user.Credits += 2;
                        _db.CreditTransactions.Add(new CreditTransaction
                        {
                            UserID = user.UserID,
                            CreditsAmount = 2,
                            ActionType = "REFUND",
                            CreatedAt = DateTime.Now
                        });
                        await _db.SaveChangesAsync();
                        
                        TempData["ErrorMessage"] = "AI service is temporarily unavailable. Your credits have been refunded. Please try again shortly.";
                        return RedirectToAction("Dashboard");
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
                    return View("ScanResult", result);
                }
                else
                {
                    TempData["ErrorMessage"] = "AI Processing Failed. Your credits have been refunded.";
                    // Refund on null result (should rarely happen)
                    user.Credits += 2;
                    _db.CreditTransactions.Add(new CreditTransaction { UserID = user.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "An error occurred during resume scanner PerformScan.");
                }
                else
                {
                    Console.WriteLine($"[Error] Scan execution failed: {ex.Message}\n{ex.StackTrace}");
                }

                // Refund on error
                user.Credits += 2;
                _db.CreditTransactions.Add(new CreditTransaction { UserID = user.UserID, CreditsAmount = 2, ActionType = "REFUND", CreatedAt = DateTime.Now });
                await _db.SaveChangesAsync();

                TempData["ErrorMessage"] = "An error occurred during the AI scan. Your credits have been refunded.";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("Dashboard");
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
