using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AIResumeScreeningSystem.Interfaces;

namespace AIResumeScreeningSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        private const string Brand = "Janala";
        private const string BrandTagline = "AI-Powered Recruitment Platform";
        private const string BrandColor = "#2563eb";
        private const string BrandDark = "#1e3a5f";

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── Shared HTML wrapper: consistent header/footer for all emails ──────────────────
        private static string WrapInTemplate(string bodyContent) => $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f1f5f9;padding:30px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;"">

          <!-- Header -->
          <tr>
            <td style=""background:{BrandColor};border-radius:8px 8px 0 0;padding:24px 32px;text-align:left;"">
              <span style=""font-size:1.5rem;font-weight:800;color:white;letter-spacing:-0.5px;"">{Brand}</span>
              <span style=""font-size:0.75rem;color:rgba(255,255,255,0.75);margin-left:8px;font-weight:500;"">{BrandTagline}</span>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""background:#ffffff;padding:32px;border-left:1px solid #e2e8f0;border-right:1px solid #e2e8f0;"">
              {bodyContent}
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 8px 8px;padding:20px 32px;text-align:center;"">
              <p style=""margin:0;font-size:0.75rem;color:#94a3b8;line-height:1.6;"">
                This email was sent by <strong style=""color:#64748b;"">{Brand}</strong> — {BrandTagline}.<br/>
                If you did not request this email, please disregard it. No action is required.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

        // ── Email Verification ─────────────────────────────────────────────────────────────
        public async Task<bool> SendVerificationEmailAsync(string email, string fullName, string token)
        {
            var baseUrl   = _config["AppBaseUrl"] ?? "https://localhost:7196";
            var verifyUrl = $"{baseUrl}/Auth/VerifyEmail?token={token}";

            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">Verify Your Email Address</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 20px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Thank you for registering with <strong>{Brand}</strong>. To activate your account and claim your
                <strong>2 complimentary credits</strong>, please verify your email address by clicking the button below.
              </p>
              <div style=""text-align:center;margin:28px 0;"">
                <a href=""{verifyUrl}"" style=""display:inline-block;padding:14px 32px;background:{BrandColor};color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;font-size:1rem;letter-spacing:0.3px;"">
                  ✔ Verify Email &amp; Activate Account
                </a>
              </div>
              <p style=""margin:0 0 8px;font-size:0.85rem;color:#6b7280;"">
                If the button does not work, copy and paste the following link into your browser:
              </p>
              <p style=""margin:0;font-size:0.8rem;color:{BrandColor};word-break:break-all;"">
                <a href=""{verifyUrl}"" style=""color:{BrandColor};"">{verifyUrl}</a>
              </p>
              <p style=""margin:24px 0 0;font-size:0.85rem;color:#9ca3af;"">
                This link will expire in <strong>24 hours</strong>.
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Please Verify Your Email Address", body);
        }

        // ── OTP Verification ──────────────────────────────────────────────────────────────
        public async Task<bool> SendOtpEmailAsync(string email, string otp)
        {
            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">Your One-Time Password (OTP)</h2>
              <p style=""margin:0 0 20px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Use the following 6-digit OTP to complete your email verification on <strong>{Brand}</strong>.
              </p>
              <div style=""text-align:center;margin:28px 0;"">
                <div style=""display:inline-block;background:#f0f4ff;border:2px solid {BrandColor};border-radius:8px;padding:18px 40px;"">
                  <span style=""font-size:2.5rem;font-weight:800;letter-spacing:14px;color:{BrandDark};font-family:'Courier New',monospace;"">{otp}</span>
                </div>
              </div>
              <p style=""margin:0 0 8px;font-size:0.9rem;color:#6b7280;text-align:center;"">
                ⏱ This OTP is valid for <strong>5 minutes</strong> only.
              </p>
              <p style=""margin:8px 0 0;font-size:0.85rem;color:#ef4444;text-align:center;"">
                Never share this OTP with anyone. {Brand} will never ask for your OTP via phone or chat.
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Your Email Verification OTP", body);
        }

        // ── Password Reset ────────────────────────────────────────────────────────────────
        public async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string token)
        {
            var baseUrl  = _config["AppBaseUrl"] ?? "https://localhost:7196";
            var resetUrl = $"{baseUrl}/Auth/ResetPassword?token={token}";

            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">Password Reset Request</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 20px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                We received a request to reset the password for your <strong>{Brand}</strong> account.
                Click the button below to set a new password. If you did not make this request, please ignore this email — your account remains secure.
              </p>
              <div style=""text-align:center;margin:28px 0;"">
                <a href=""{resetUrl}"" style=""display:inline-block;padding:14px 32px;background:#dc2626;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;font-size:1rem;"">
                  🔐 Reset My Password
                </a>
              </div>
              <p style=""margin:0 0 8px;font-size:0.85rem;color:#6b7280;"">
                Or copy and paste this link into your browser:
              </p>
              <p style=""margin:0;font-size:0.8rem;color:{BrandColor};word-break:break-all;"">
                <a href=""{resetUrl}"" style=""color:{BrandColor};"">{resetUrl}</a>
              </p>
              <p style=""margin:24px 0 0;font-size:0.85rem;color:#9ca3af;"">
                This reset link will expire in <strong>1 hour</strong>. If you need a new link, please visit the login page and request another reset.
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Password Reset Request", body);
        }

        // ── Application Confirmation ──────────────────────────────────────────────────────
        public async Task<bool> SendApplicationConfirmationAsync(string email, string fullName, string jobTitle)
        {
            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">Application Successfully Submitted</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 16px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Your application for the position of <strong>{jobTitle}</strong> has been successfully received.
                Our AI-powered screening system is now reviewing your profile and matching it against the job requirements.
              </p>
              <div style=""background:#f0fdf4;border-left:4px solid #16a34a;border-radius:4px;padding:14px 18px;margin:20px 0;"">
                <p style=""margin:0;font-size:0.9rem;color:#166534;font-weight:600;"">
                  ✅ Your application is under review. You will be notified of any updates by email.
                </p>
              </div>
              <p style=""margin:0;font-size:0.85rem;color:#6b7280;"">
                We appreciate your interest and wish you the very best in your application. Thank you for using <strong>{Brand}</strong>.
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Application Received: {jobTitle}", body);
        }

        // ── Shortlist Notification ────────────────────────────────────────────────────────
        public async Task<bool> SendShortlistNotificationAsync(string email, string fullName, string jobTitle)
        {
            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">🎉 You Have Been Shortlisted!</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 16px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                We are delighted to inform you that your profile has been <strong>shortlisted</strong> for the
                <strong>{jobTitle}</strong> position. This is a significant step forward in your application.
              </p>
              <div style=""background:#eff6ff;border-left:4px solid {BrandColor};border-radius:4px;padding:14px 18px;margin:20px 0;"">
                <p style=""margin:0;font-size:0.9rem;color:#1d4ed8;font-weight:600;"">
                  📋 The recruitment team will be in touch with you shortly regarding the next steps.
                </p>
              </div>
              <p style=""margin:0;font-size:0.85rem;color:#6b7280;"">
                Please ensure your contact details are up to date on your <strong>{Brand}</strong> profile.
                Congratulations and best of luck!
              </p>");

            return await SendEmailAsync(email, $"{Brand} — You've Been Shortlisted for {jobTitle}", body);
        }

        // ── Rejection Notification ────────────────────────────────────────────────────────
        public async Task<bool> SendRejectionEmailAsync(string email, string fullName, string jobTitle)
        {
            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">Application Status Update</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 16px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Thank you for your interest in the <strong>{jobTitle}</strong> position and for taking the time to apply through <strong>{Brand}</strong>.
                After a thorough review of all applications, we regret to inform you that we have decided to move forward with other candidates whose profiles more closely align with our current requirements.
              </p>
              <p style=""margin:0 0 16px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                This decision does not reflect negatively on your qualifications. We strongly encourage you to continue exploring other opportunities on our platform.
              </p>
              <p style=""margin:0;font-size:0.85rem;color:#6b7280;"">
                We wish you every success in your career journey and hope to be of service to you again in the future.
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Application Update: {jobTitle}", body);
        }

        // ── Hired Confirmation ────────────────────────────────────────────────────────────
        public async Task<bool> SendHiredConfirmationAsync(string email, string fullName, string jobTitle)
        {
            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:#16a34a;"">🎊 Congratulations — You're Hired!</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 16px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                We are thrilled to officially confirm that you have been selected for the position of
                <strong>{jobTitle}</strong>. This is a wonderful achievement and we warmly welcome you aboard!
              </p>
              <div style=""background:#f0fdf4;border-left:4px solid #16a34a;border-radius:4px;padding:14px 18px;margin:20px 0;"">
                <p style=""margin:0;font-size:0.9rem;color:#166534;font-weight:600;"">
                  🌟 The employer's HR team will contact you with your official offer letter and onboarding details.
                </p>
              </div>
              <p style=""margin:0;font-size:0.85rem;color:#6b7280;"">
                Congratulations once again from the entire <strong>{Brand}</strong> team. We are proud to have been part of your career success story!
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Congratulations, You're Hired! {jobTitle}", body);
        }

        // ── Interview Invitation ──────────────────────────────────────────────────────────
        public async Task<bool> SendInterviewInvitationAsync(string email, string fullName, string jobTitle, DateTime date, TimeSpan time, string type, string? link)
        {
            var linkRow = string.IsNullOrEmpty(link) ? "" :
                $@"<tr>
                    <td style=""padding:6px 0;font-size:0.9rem;color:#374151;""><strong>Meeting Link:</strong></td>
                    <td style=""padding:6px 0;font-size:0.9rem;""><a href=""{link}"" style=""color:{BrandColor};"">{link}</a></td>
                   </tr>";

            var body = WrapInTemplate($@"
              <h2 style=""margin:0 0 16px;font-size:1.4rem;font-weight:700;color:{BrandDark};"">📅 Interview Invitation</h2>
              <p style=""margin:0 0 12px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                Dear <strong>{fullName}</strong>,
              </p>
              <p style=""margin:0 0 20px;color:#374151;font-size:0.95rem;line-height:1.6;"">
                You are cordially invited to attend an interview for the position of <strong>{jobTitle}</strong>.
                Please review the details below and confirm your availability.
              </p>
              <table cellpadding=""0"" cellspacing=""0"" style=""width:100%;background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;padding:16px;margin-bottom:20px;"">
                <tr>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;white-space:nowrap;""><strong>Position:</strong></td>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;"">{jobTitle}</td>
                </tr>
                <tr>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;""><strong>Date:</strong></td>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;"">{date:dddd, MMMM dd, yyyy}</td>
                </tr>
                <tr>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;""><strong>Time:</strong></td>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;"">{time:hh\\:mm} (Local Time)</td>
                </tr>
                <tr>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;""><strong>Format:</strong></td>
                  <td style=""padding:6px 16px;font-size:0.9rem;color:#374151;"">{type}</td>
                </tr>
                {linkRow}
              </table>
              <p style=""margin:0;font-size:0.85rem;color:#6b7280;"">
                If you have any questions or need to reschedule, please contact the recruitment team directly.
                We look forward to speaking with you. Best of luck!
              </p>");

            return await SendEmailAsync(email, $"{Brand} — Interview Invitation: {jobTitle}", body);
        }

        // ── Core SMTP Sender ──────────────────────────────────────────────────────────────
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            try
            {
                var from        = _config["EmailSettings:From"]        ?? throw new InvalidOperationException("EmailSettings:From is not configured.");
                var displayName = _config["EmailSettings:DisplayName"] ?? Brand;
                var smtpHost    = _config["EmailSettings:SmtpServer"]  ?? throw new InvalidOperationException("EmailSettings:SmtpServer is not configured.");
                var port        = int.Parse(_config["EmailSettings:Port"] ?? "587");
                var username    = _config["EmailSettings:Username"]    ?? from; // Gmail: username = the from address
                var password    = _config["EmailSettings:Password"]    ?? throw new InvalidOperationException("EmailSettings:Password is not configured.");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(displayName, from));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
                message.Body = bodyBuilder.ToMessageBody();

                _logger.LogInformation("Attempting to send email to {Email} via SMTP ({SmtpHost}:{Port})...", toEmail, smtpHost, port);

                using var client = new SmtpClient();
                // 30-second timeout to prevent indefinite hangs
                client.Timeout = 30000;
                await client.ConnectAsync(smtpHost, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email successfully sent to {Email} — Subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} — Subject: {Subject}", toEmail, subject);
                return false;
            }
        }
    }
}
