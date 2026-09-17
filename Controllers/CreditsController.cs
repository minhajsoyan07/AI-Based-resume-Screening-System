using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Constants;
using Microsoft.EntityFrameworkCore;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize]
    public class CreditsController : BaseController
    {
        private readonly ApplicationDbContext _db;

        public CreditsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _db.Users.FindAsync(CurrentUserID);
            if (user == null) return Unauthorized();

            ViewBag.CurrentCredits = user.Credits;
            ViewBag.IsVerified = user.EmailVerified;
            
            var packages = new List<TokenPackage>
            {
                new TokenPackage { ID = 1, Name = "Starter Pack", Credits = 20, PriceBDT = 100, Description = "Perfect for casual job seekers exploring opportunities.", IsPopular = false },
                new TokenPackage { ID = 2, Name = "Professional", Credits = 45, PriceBDT = 200, Description = "Ideal for active applicants actively interviewing.", IsPopular = true },
                new TokenPackage { ID = 3, Name = "Power User", Credits = 120, PriceBDT = 500, Description = "Best for aggressive job hunting and career building.", IsPopular = false }
            };

            return View(packages);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int packageId)
        {
            var packages = new List<TokenPackage>
            {
                new TokenPackage { ID = 1, Name = "Starter Pack", Credits = 20, PriceBDT = 100 },
                new TokenPackage { ID = 2, Name = "Professional", Credits = 45, PriceBDT = 200 },
                new TokenPackage { ID = 3, Name = "Power User", Credits = 120, PriceBDT = 500 }
            };

            var selectedPackage = packages.FirstOrDefault(p => p.ID == packageId);
            if (selectedPackage == null) return RedirectToAction("Index");

            return View(selectedPackage);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int packageId, string paymentMethod)
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var packages = new List<TokenPackage>
            {
                new TokenPackage { ID = 1, Credits = 20 },
                new TokenPackage { ID = 2, Credits = 45 },
                new TokenPackage { ID = 3, Credits = 120 }
            };

            var selectedPackage = packages.FirstOrDefault(p => p.ID == packageId);
            if (selectedPackage == null) return BadRequest("Invalid package");

            // Simulate Payment Logic
            // In a real app, you would integrate bKash/Nagad/SSLCommerz here.
            
            user.Credits += selectedPackage.Credits;
            
            _db.CreditTransactions.Add(new CreditTransaction
            {
                UserID = (int)userId,
                CreditsAmount = selectedPackage.Credits,
                AmountBDT = packageId switch { 1 => 100, 2 => 200, 3 => 500, _ => 0 },
                PaymentMethod = paymentMethod,
                ActionType = "PURCHASE",
                AdminVerified = true, // Auto-verify for simulation
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Successfully purchased {selectedPackage.Credits} credits via {paymentMethod}!";
            return RedirectToAction("Index");
        }
    }

    public class TokenPackage
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public int Credits { get; set; }
        public decimal PriceBDT { get; set; }
        public string Description { get; set; } = "";
        public bool IsPopular { get; set; }
    }
}
