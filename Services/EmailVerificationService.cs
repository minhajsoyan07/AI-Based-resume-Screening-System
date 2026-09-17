using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIResumeScreeningSystem.Interfaces;

namespace AIResumeScreeningSystem.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(IConfiguration config, HttpClient httpClient, ILogger<EmailVerificationService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> IsValidEmailAsync(string email)
        {
            var apiKey = Environment.GetEnvironmentVariable("MJ_APIKEY_PUBLIC") ?? _config["EmailSettings:Mailjet:ApiKey"];
            var secretKey = Environment.GetEnvironmentVariable("MJ_APIKEY_PRIVATE") ?? _config["EmailSettings:Mailjet:SecretKey"];
            var baseUrl = Environment.GetEnvironmentVariable("MJ_BASE_URL") ?? _config["EmailSettings:Mailjet:BaseUrl"] ?? "https://api.mailjet.com";
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogWarning("Email verification skipped: Mailjet credentials not configured.");
                return true; // Fallback to avoid blocking users
            }

            try
            {
                var requestPayload = new { Email = email };
                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}"));
                
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v3/REST/emailvalidation");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    // Mailjet Validation Result Parsing
                    if (root.TryGetProperty("Verdict", out var verdictProp))
                    {
                        var verdict = verdictProp.GetString()?.ToLowerInvariant();
                        _logger.LogInformation("Mailjet verification for {Email}: Verdict={Verdict}", email, verdict);

                        // Allow Deliverable and Unknown (to avoid false negatives)
                        // Block Undeliverable, Risky, or Disposable if supported
                        return verdict == "deliverable" || verdict == "unknown" || verdict == "catch_all";
                    }

                    return true; // If Verdict is missing but status is 200, assume OK
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Mailjet Verification API Error (HTTP {Status}): {Error}", (int)response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Mailjet Email Verification for {Email}", email);
            }

            return true; // Fallback to avoid blocking users if API is down
        }
    }
}
