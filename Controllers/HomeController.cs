using Microsoft.AspNetCore.Mvc;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly IJobService _jobs;
        private readonly ApplicationDbContext _db;
        private readonly IResumeParserService _parser;
        private readonly IAIMatchingService _matcher;

        public HomeController(IJobService jobs, ApplicationDbContext db, IResumeParserService parser, IAIMatchingService matcher)
        {
            _jobs = jobs;
            _db = db;
            _parser = parser;
            _matcher = matcher;
        }

        public async Task<IActionResult> Index()
        {
            // Authenticated users go straight to their dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                return role?.ToUpper() switch
                {
                    "ADMIN"     => RedirectToAction("Index", "Admin"),
                    "RECRUITER" => RedirectToAction("Index", "Recruiter"),
                    "APPLICANT" => RedirectToAction("Index", "Applicant"),
                    _           => RedirectToAction("Index", "Applicant")
                };
            }

            var jobs = await _db.Jobs.ToListAsync();
            return View(jobs);
        }

        public IActionResult HowItWorks()
        {
            return View();
        }

        public IActionResult ForRecruiters()
        {
            return View();
        }

        public IActionResult ContactUs()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
