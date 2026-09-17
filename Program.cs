using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ── ENVIRONMENT VARIABLES FOR SECRETS ──
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JwtSettings:SecretKey"] 
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required");

// ── DATABASE ──
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"];
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured. Please set ConnectionStrings:DefaultConnection in appsettings.json, appsettings.Development.json, or environment variables.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(defaultConnection, sqlOptions => {
        sqlOptions.CommandTimeout(60); // Increase to 60 seconds for AI operations
        sqlOptions.EnableRetryOnFailure(3); // Retry on transient failures
    }));

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── MVC + RAZOR PAGES ──
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

// ── PERFORMANCE: RESPONSE COMPRESSION ──
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "image/svg+xml", "application/json" });
    });
}

// ── HTTP CONTEXT & CACHE ──
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RecruitAI.Session";
});

// ── RESPONSE CACHING ──
builder.Services.AddResponseCaching();

// ── COOKIE AUTHENTICATION ──
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath         = "/Auth/ApplicantLogin"; // default
        options.LogoutPath        = "/Auth/Logout";
        options.AccessDeniedPath  = "/Auth/ApplicantLogin";
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite   = SameSiteMode.Strict;
        options.Cookie.Name       = ".ResuMatch.Auth";
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // AJAX requests get 401
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }

                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                string loginUrl = path switch
                {
                    _ when path.StartsWith("/portal/admin") || path.StartsWith("/admin")
                        => "/Auth/AdminLogin",
                    _ when path.StartsWith("/recruiter")
                        => "/Auth/RecruiterLogin",
                    _ when path.StartsWith("/applicant")
                        => "/Auth/ApplicantLogin",
                    _   => "/Auth/ApplicantLogin"
                };

                // Preserve return URL
                var redirectUri = $"{loginUrl}?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}";
                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                // Role mismatch: redirect to the correct login for the attempted path
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                string loginUrl = path switch
                {
                    _ when path.StartsWith("/portal/admin") || path.StartsWith("/admin")
                        => "/Auth/AdminLogin",
                    _ when path.StartsWith("/recruiter")
                        => "/Auth/RecruiterLogin",
                    _   => "/Auth/ApplicantLogin"
                };
                context.Response.Redirect(loginUrl);
                return Task.CompletedTask;
            }
        };
    });

// ── GLOBAL AUTHORIZATION ──
builder.Services.AddAuthorization(options =>
{
    // Require authenticated user by default
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ── SERVICES (DI) ──
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<AdminManagementService>(); // Admin panel service

// Typed HTTP Clients for AI and External Services
builder.Services.AddHttpClient(); // For general use
builder.Services.AddHttpClient<IAIMatchingService, AIMatchingService>();
builder.Services.AddHttpClient<IAIResumeService, GeminiResumeService>();
builder.Services.AddHttpClient<IResumeParserService, ResumeParserService>();
builder.Services.AddScoped<StoredProcedureService>(); // _janala stored procedures wrapper

// ── ANTIFORGERY ──
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ──
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 7; // 7 days
        ctx.Context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});
app.UseCors("AllowAll");
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ── PANEL ACCESS GUARD (Middleware) ──────────────────────────────────
// Ensures each user can only access their own panel.
// Runs AFTER UseAuthentication/UseAuthorization so claims are available.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToUpperInvariant() ?? "";
    var isStatic = path.StartsWith("/UPLOADS") || path.StartsWith("/CSS") ||
                   path.StartsWith("/JS")      || path.StartsWith("/LIB") ||
                   path.StartsWith("/IMAGES")  || path.StartsWith("/FAVICON");
    if (isStatic) { await next(); return; }

    var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    var auth   = context.User.Identity?.IsAuthenticated == true;
    var role   = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpperInvariant();

    // --- Unauthenticated: redirect to the right login page ---------------
    if (!auth)
    {
        string? loginDest = null;
        if (path.StartsWith("/ADMIN") || path.StartsWith("/PORTAL/ADMIN"))
            loginDest = "/Auth/AdminLogin";
        else if (path.StartsWith("/RECRUITER"))
            loginDest = "/Auth/RecruiterLogin";
        else if (path.StartsWith("/APPLICANT"))
            loginDest = "/Auth/ApplicantLogin";

        if (loginDest != null)
        {
            if (isAjax) { context.Response.StatusCode = 401; return; }
            context.Response.Redirect($"{loginDest}?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            return;
        }

        await next(); return;
    }

    // --- Authenticated: block cross-panel access -------------------------
    bool crossPanel = false;
    string redirectTo = "/";

    if ((path.StartsWith("/ADMIN") || path.StartsWith("/PORTAL/ADMIN")) && role != "ADMIN")
    {
        crossPanel = true;
        // If the user is a recruiter/applicant trying admin → send them home
        redirectTo = role == "RECRUITER" ? "/Recruiter" : "/Applicant";
    }
    else if (path.StartsWith("/RECRUITER") && role != "RECRUITER")
    {
        crossPanel = true;
        redirectTo = role == "ADMIN" ? "/portal/admin" : "/Applicant";
    }
    else if (path.StartsWith("/APPLICANT") && role != "APPLICANT")
    {
        crossPanel = true;
        redirectTo = role == "ADMIN" ? "/portal/admin" : "/Recruiter";
    }

    if (crossPanel)
    {
        if (isAjax) { context.Response.StatusCode = 403; return; }
        context.Response.Redirect(redirectTo);
        return;
    }

    await next();
});

// ── CREATE UPLOAD FOLDER ──
var resumesPath = Path.Combine(app.Environment.WebRootPath, "uploads", "resumes");
var jobsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "jobs");
Directory.CreateDirectory(resumesPath);
Directory.CreateDirectory(jobsPath);

// ── AUTO MIGRATE & SEED ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");

        // Optimized Fix for Existing Jobs: Batch update attachment paths directly in DB
        logger.LogInformation("Patching existing jobs with missing attachments...");
        db.Jobs
          .Where(j => string.IsNullOrEmpty(j.AttachmentPath))
          .ExecuteUpdate(setters => setters.SetProperty(j => j.AttachmentPath, "/uploads/jobs/sample_circular.pdf"));
        logger.LogInformation("Job patching completed.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Skipping database migration/seeding because the database is already fully created or locked.");
    }
}

// ── ROUTES ──
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
