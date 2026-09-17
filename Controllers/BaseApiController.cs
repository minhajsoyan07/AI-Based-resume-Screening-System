using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AIResumeScreeningSystem.Constants;

namespace AIResumeScreeningSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected int? CurrentUserID
        {
            get
            {
                var userId = HttpContext.Session.GetInt32(SessionKeys.UserID);
                if (userId == null && User.Identity?.IsAuthenticated == true)
                {
                    SyncSessionFromClaims();
                    userId = HttpContext.Session.GetInt32(SessionKeys.UserID);
                }
                return userId;
            }
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        protected string? CurrentUserRole => HttpContext.Session.GetString(SessionKeys.UserRole) ?? User.FindFirstValue(ClaimTypes.Role);

        private void SyncSessionFromClaims()
        {
            if (User.Identity?.IsAuthenticated == true && HttpContext.Session.GetInt32(SessionKeys.UserID) == null)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    HttpContext.Session.SetInt32(SessionKeys.UserID, userId);
                    HttpContext.Session.SetString(SessionKeys.UserRole, User.FindFirstValue(ClaimTypes.Role) ?? "");
                    HttpContext.Session.SetString(SessionKeys.UserName, User.Identity.Name ?? "");
                    HttpContext.Session.SetString(SessionKeys.UserEmail, User.FindFirstValue(ClaimTypes.Email) ?? "");
                }
            }
        }
    }
}
