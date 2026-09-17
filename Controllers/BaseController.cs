using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using AIResumeScreeningSystem.Constants;

namespace AIResumeScreeningSystem.Controllers
{
    public abstract class BaseController : Controller
    {
        // ── Identity helpers ─────────────────────────────────────────

        protected int? CurrentUserID
        {
            get
            {
                // Prefer session (fast), fall back to claims, then sync
                var id = HttpContext.Session.GetInt32(SessionKeys.UserID);
                if (id == null && User.Identity?.IsAuthenticated == true)
                {
                    SyncSessionFromClaims();
                    id = HttpContext.Session.GetInt32(SessionKeys.UserID);
                }
                return id;
            }
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        protected string? CurrentUserRole =>
            HttpContext.Session.GetString(SessionKeys.UserRole)
            ?? User.FindFirstValue(ClaimTypes.Role);

        protected string? CurrentUserName =>
            HttpContext.Session.GetString(SessionKeys.UserName)
            ?? User.Identity?.Name;

        protected string? CurrentUserEmail =>
            HttpContext.Session.GetString(SessionKeys.UserEmail)
            ?? User.FindFirstValue(ClaimTypes.Email);

        // ── Role-based home URL ──────────────────────────────────────
        protected string UserHomeUrl => CurrentUserRole?.ToUpper() switch
        {
            "ADMIN"     => "/portal/admin",
            "RECRUITER" => "/Recruiter",
            "APPLICANT" => "/Applicant",
            _           => "/"
        };

        // ── Filter: runs on every action, keeps ViewBag fresh ────────
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Ensure session is always in sync with the auth cookie
                SyncSessionFromClaims();

                ViewBag.IsAuthenticated = true;
                ViewBag.UserHome        = UserHomeUrl;
                ViewBag.UserRole        = CurrentUserRole;
                ViewBag.UserName        = CurrentUserName;
                ViewBag.UserEmail       = CurrentUserEmail;
                ViewBag.UserID          = CurrentUserID;
                // Credits from claim (set at login) — avoids a DB call on every page
                ViewBag.Credits = User.FindFirstValue("Credits") ?? "0";
            }
            else
            {
                ViewBag.IsAuthenticated = false;
                ViewBag.UserHome        = "/";
                ViewBag.UserName        = null;
                ViewBag.UserRole        = null;
                ViewBag.UserID          = null;
            }

            base.OnActionExecuting(context);
        }

        // ── Session sync (claims → session) ─────────────────────────
        /// <summary>
        /// Always syncs claims into session so the session is never stale
        /// when the auth cookie is still valid (e.g. after server restart).
        /// Only writes when something is missing to avoid redundant work.
        /// </summary>
        private void SyncSessionFromClaims()
        {
            if (User.Identity?.IsAuthenticated != true) return;

            // Always refresh name in case it was updated via profile edit
            var claimName  = User.Identity.Name ?? "";
            var claimEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var claimRole  = User.FindFirstValue(ClaimTypes.Role) ?? "";
            var claimIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            if (!int.TryParse(claimIdStr, out int userId)) return;

            // Write all keys every time so header/layout is always accurate
            HttpContext.Session.SetInt32(SessionKeys.UserID,    userId);
            HttpContext.Session.SetString(SessionKeys.UserName,  claimName);
            HttpContext.Session.SetString(SessionKeys.UserEmail, claimEmail);
            HttpContext.Session.SetString(SessionKeys.UserRole,  claimRole);
        }

        // ── Session write (used at login) ────────────────────────────
        protected void SetSessionData(int userId, string role, string name, string email)
        {
            HttpContext.Session.SetInt32(SessionKeys.UserID,    userId);
            HttpContext.Session.SetString(SessionKeys.UserRole,  role);
            HttpContext.Session.SetString(SessionKeys.UserName,  name);
            HttpContext.Session.SetString(SessionKeys.UserEmail, email);
        }

        /// <summary>
        /// Call this after a profile update to instantly refresh the session/header
        /// without requiring the user to re-login.
        /// </summary>
        protected void RefreshSessionName(string newName)
        {
            HttpContext.Session.SetString(SessionKeys.UserName, newName);
            ViewBag.UserName = newName;
        }

        // ── Session clear (used at logout) ───────────────────────────
        protected void ClearSession() => HttpContext.Session.Clear();
    }
}
