using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace AIResumeScreeningSystem.Controllers
{
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthApiController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuthService _auth;
        private readonly ILogger<AuthApiController> _logger;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly IEmailService _email;

        public AuthApiController(ApplicationDbContext db, IAuthService auth, ILogger<AuthApiController> logger, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, IEmailService email)
        {
            _db = db;
            _auth = auth;
            _logger = logger;
            _cache = cache;
            _email = email;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>(false, "Invalid input data"));

            var (success, message) = await _auth.RegisterAsync(model);

            if (!success)
                return BadRequest(new ApiResponse<object>(false, message));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            var token = _auth.GenerateJwtToken(user!);

            return Ok(new ApiResponse<object>(true, "Registration successful", new
            {
                userId = user!.UserID,
                email = user.Email,
                role = user.Role,
                token
            }));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>(false, "Invalid input data"));

            var (success, message, user) = await _auth.LoginAsync(model);

            if (!success || user == null)
                return Unauthorized(new ApiResponse<object>(false, message));

            var token = _auth.GenerateJwtToken(user);

            return Ok(new ApiResponse<object>(true, "Login successful", new
            {
                userId = user.UserID,
                email = user.Email,
                role = user.Role,
                fullName = user.FullName,
                token
            }));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new ApiResponse<object>(false, "User not found"));

            return Ok(new ApiResponse<object>(true, null, new
            {
                userId = user.UserID,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role,
                createdDate = user.CreatedDate
            }));
        }

        [HttpPost("refresh")]
        [Authorize]
        public IActionResult RefreshToken()
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var user = _db.Users.Find(userId);
            
            if (user == null)
                return NotFound(new ApiResponse<object>(false, "User not found"));

            var newToken = _auth.GenerateJwtToken(user);

            return Ok(new ApiResponse<object>(true, null, new { token = newToken }));
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new ApiResponse<object>(true, "Logged out successfully"));
        }
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>(false, "Invalid input data"));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.AccountStatus == "Active");
            if (user != null)
            {
                var token = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);
                
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                await _email.SendPasswordResetEmailAsync(user.Email, user.FullName, token);
            }

            return Ok(new ApiResponse<object>(true, "If your email is registered in our system, you will receive a password reset link shortly."));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>(false, "Invalid input data"));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == model.Token && u.PasswordResetTokenExpiry > DateTime.Now && u.AccountStatus == "Active");
            if (user == null)
                return BadRequest(new ApiResponse<object>(false, "This password reset link is invalid or has expired. Please request a new one."));

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedDate = DateTime.Now;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new ApiResponse<object>(true, "Your password has been reset successfully."));
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new ApiResponse<object>(false, "Email is required"));
            
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            _cache.Set($"OTP_{request.Email.ToLower()}", otp, TimeSpan.FromMinutes(5));

            var success = await _email.SendOtpEmailAsync(request.Email, otp);
            if (!success) return StatusCode(500, new ApiResponse<object>(false, "Failed to send OTP email"));

            return Ok(new ApiResponse<object>(true, "OTP sent successfully"));
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new ApiResponse<object>(false, "Email and OTP are required"));

            if (_cache.TryGetValue($"OTP_{request.Email.ToLower()}", out string? cachedOtp))
            {
                if (cachedOtp == request.Otp)
                {
                    _cache.Remove($"OTP_{request.Email.ToLower()}"); // OTP can only be used once
                    return Ok(new ApiResponse<object>(true, "OTP verified successfully"));
                }
            }
            return BadRequest(new ApiResponse<object>(false, "Invalid or expired OTP"));
        }
    }

    public class SendOtpRequest { public string Email { get; set; } = string.Empty; }
    public class VerifyOtpRequest { public string Email { get; set; } = string.Empty; public string Otp { get; set; } = string.Empty; }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public ApiResponse(bool success, string? message = null, T? data = default)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }
}