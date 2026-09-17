using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AIResumeScreeningSystem.Interfaces;
using System.Net.Http.Json;
using UglyToad.PdfPig;
using Google.GenAI;
using Google.GenAI.Types;
using IO = System.IO; // disambiguate from Google.GenAI.Types.File

namespace AIResumeScreeningSystem.Services
{
    /// <summary>
    /// Parses resume files (PDF/DOCX/Images) and extracts structured data using the Gemini AI API.
    /// Follows the official Google.GenAI SDK quickstart — reads GOOGLE_API_KEY from environment.
    /// </summary>
    public class ResumeParserService : IResumeParserService
    {
        private readonly ILogger<ResumeParserService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        private const string ParseModel = "gemini-1.5-flash-latest";

        public ResumeParserService(ILogger<ResumeParserService> logger, IConfiguration config, HttpClient httpClient)
        {
            _logger      = logger;
            _config      = config;
            _httpClient  = httpClient;
        }

        // ══════════════════════════════════════════════════════════════
        // ParseResumeAsync — file path version (used after registration)
        // ══════════════════════════════════════════════════════════════
        public async Task<ParsedResumeData> ParseResumeAsync(string filePath)
        {
            var result   = new ParsedResumeData();
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));

            if (!IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Resume file not found: {Path}", fullPath);
                return result;
            }

            try
            {
                // Step 1: Extract raw text from file
                string rawText = ExtractLocalText(fullPath);
                result.RawText = rawText;

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogWarning("No text extracted from {File}. Skipping AI parse.", filePath);
                    return result;
                }

                // Step 2: Send to AI fallback chain for structured extraction
                return await ParseTextWithAIAsync(rawText, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResumeParserService.ParseResumeAsync failed for {File}", filePath);
                result.ExtractionIssues.Add($"Parse error: {ex.Message}");
                return result;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ExtractTextAsync — IFormFile version (used during upload/scan)
        // ══════════════════════════════════════════════════════════════
        public async Task<string> ExtractTextAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0) return "";

            var ext = IO.Path.GetExtension(file.FileName).ToLowerInvariant();

            // Images must use Vision
            if (ext is ".png" or ".jpg" or ".jpeg")
                return await ExtractViaVisionAsync(file);

            // PDF/DOCX: try local extraction first, fall back to Vision
            var tempPath = IO.Path.GetTempFileName() + ext;
            try
            {
                await using (var fs = new IO.FileStream(tempPath, IO.FileMode.Create))
                    await file.CopyToAsync(fs);

                var text = ExtractLocalText(tempPath);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogInformation("Local extraction empty for {File}, trying AI Vision fallback.", file.FileName);
                    text = await ExtractViaVisionAsync(file);
                }

                return text;
            }
            finally
            {
                if (IO.File.Exists(tempPath)) IO.File.Delete(tempPath);
            }
        }

        public async Task<string> ExtractTextFromPathAsync(string filePath)
        {
            if (!IO.File.Exists(filePath)) return "";

            var ext = IO.Path.GetExtension(filePath).ToLowerInvariant();

            // Images must use Vision
            if (ext is ".png" or ".jpg" or ".jpeg")
                return await ExtractViaVisionAsync(await IO.File.ReadAllBytesAsync(filePath), filePath);

            // PDF/DOCX: try local extraction first, fall back to Vision
            var text = ExtractLocalText(filePath);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogInformation("Local extraction empty for {File}, trying AI Vision fallback.", filePath);
                text = await ExtractViaVisionAsync(await IO.File.ReadAllBytesAsync(filePath), filePath);
            }

            return text;
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Extracts text locally from PDF or DOCX without calling any API.
        /// </summary>
        private string ExtractLocalText(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".pdf")
            {
                var sb = new System.Text.StringBuilder();
                try
                {
                    using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
                    foreach (var page in doc.GetPages())
                        sb.AppendLine(page.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("PdfPig extraction failed: {Msg}", ex.Message);
                }
                return sb.ToString();
            }

            if (ext == ".docx")
            {
                var sb = new System.Text.StringBuilder();
                try
                {
                    using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false);
                    var body = doc.MainDocumentPart?.Document?.Body;
                    if (body != null)
                        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                            sb.AppendLine(para.InnerText);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("DOCX extraction failed: {Msg}", ex.Message);
                }
                return sb.ToString();
            }

            return "";
        }

        /// <summary>
        /// Calls AI Vision fallback chain to extract text from image/PDF files.
        /// </summary>
        private async Task<string> ExtractViaVisionAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                return await ExtractViaVisionAsync(ms.ToArray(), file.FileName);
            }
        }

        private async Task<string> ExtractViaVisionAsync(byte[] bytes, string fileName)
        {
            var errors = new List<string>();

            var order = new List<string> { "OpenAI", "Gemini", "OpenRouter", "Local" };

            foreach (var provider in order)
            {
                try
                {
                    var result = await TryVisionProviderAsync(provider, bytes, fileName);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        _logger.LogInformation("Vision extraction succeeded using provider: {Provider}", provider);
                        return result;
                    }
                    errors.Add($"{provider}: returned empty response");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AI vision provider {Provider} failed: {Error}. Trying next fallback.", provider, ex.Message);
                    errors.Add($"{provider}: {ex.Message}");
                }
            }

            _logger.LogError("All AI vision providers failed. Details: {Details}", string.Join(" | ", errors));
            return string.Empty;
        }

        private async Task<string> TryVisionProviderAsync(string provider, byte[] bytes, string fileName)
        {
            var mimeType = Path.GetExtension(fileName).ToLower() switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _       => "application/octet-stream"
            };

            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return "Extracted document text placeholder (Offline Mode).";
            }

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey  = _config["AISettings:OpenAI:ApiKey"];
                var baseUrl = _config["AISettings:OpenAI:BaseUrl"] ?? "https://rkapi.com/v1";
                var model   = _config["AISettings:OpenAI:Model"] ?? "gpt-5.5";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenAI ApiKey is missing.");

                string endpoint = baseUrl.TrimEnd('/') + "/chat/completions";

                object contentPayload;
                if (mimeType == "application/pdf")
                {
                    contentPayload = new object[]
                    {
                        new { type = "text", text = "Extract all text from this document accurately. Return only the extracted text, no commentary." },
                        new { type = "file", file = new { filename = fileName, file_data = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}" } }
                    };
                }
                else
                {
                    contentPayload = new object[]
                    {
                        new { type = "text", text = "Extract all text from this image accurately. Return only the extracted text, no commentary." },
                        new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}" } }
                    };
                }

                var requestBody = new
                {
                    model    = model,
                    messages = new[] { new { role = "user", content = contentPayload } }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenAI HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    var errMsg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    throw new Exception($"OpenAI API error: {errMsg}");
                }

                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                {
                    throw new Exception($"OpenAI response is missing 'choices'. Full response: {body}");
                }

                var text = choicesEl[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return text;
            }

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("Gemini ApiKey is missing.");

                var client = new Client(apiKey: apiKey);
                var response = await client.Models.GenerateContentAsync(
                    model: _config["AISettings:Gemini:Model"] ?? ParseModel,
                    contents: new List<Content>
                    {
                        new Content
                        {
                            Parts = new List<Part>
                            {
                                new Part { Text = "Extract all text from this document/image accurately. Return only the extracted text, no commentary." },
                                new Part { InlineData = new Blob { MimeType = mimeType, Data = bytes } }
                            }
                        }
                    }
                );

                return response?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? string.Empty;
            }
            else if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:OpenRouter:ApiKey"];
                var model  = _config["AISettings:OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenRouter ApiKey is missing.");

                object contentPayload;
                if (mimeType == "application/pdf")
                {
                    contentPayload = new object[]
                    {
                        new { type = "text", text = "Extract all text from this document accurately. Return only the extracted text, no commentary." },
                        new { type = "file", file = new { filename = fileName, file_data = $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}" } }
                    };
                }
                else
                {
                    contentPayload = new object[]
                    {
                        new { type = "text", text = "Extract all text from this image accurately. Return only the extracted text, no commentary." },
                        new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}" } }
                    };
                }

                var requestBody = new
                {
                    model    = model,
                    messages = new[] { new { role = "user", content = contentPayload } }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("HTTP-Referer", "http://localhost:5000");
                request.Headers.Add("X-Title", "AIBasedResumeScreeningSystem");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenRouter HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    var errMsg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    throw new Exception($"OpenRouter API error: {errMsg}");
                }

                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                {
                    throw new Exception($"OpenRouter response is missing 'choices'. Full response: {body}");
                }

                var text = choicesEl[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return text;
            }


            throw new Exception($"Unknown vision provider: {provider}");
        }

        /// <summary>
        /// Sends raw resume text to AI fallback chain and receives a fully structured ParsedResumeData.
        /// </summary>
        private async Task<ParsedResumeData> ParseTextWithAIAsync(string rawText, ParsedResumeData fallback)
        {
            var prompt = @"You are an expert resume parser. Extract the following information from the resume text below.
Respond ONLY with a valid JSON object matching this exact schema. No markdown, no backticks, no comments:
{
  ""Name"": """",
  ""Email"": """",
  ""Phone"": """",
  ""Address"": """",
  ""LinkedInURL"": """",
  ""GitHubURL"": """",
  ""PortfolioURL"": """",
  ""Skills"": [],
  ""PrimarySkills"": [],
  ""SecondarySkills"": [],
  ""HighestEducation"": """",
  ""YearsOfExperience"": 0,
  ""CurrentJobTitle"": """",
  ""ProfileSummary"": """",
  ""Certifications"": [],
  ""Languages"": [],
  ""Projects"": [],
  ""Achievements"": [],
  ""Industries"": [],
  ""Tools"": [],
  ""SoftSkills"": []
}

Resume Text:
" + TruncateText(rawText, 15000);

            try
            {
                var jsonText = await GenerateTextAsync(prompt, 0.1f, true);
                if (string.IsNullOrWhiteSpace(jsonText)) return fallback;

                var options  = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed   = JsonSerializer.Deserialize<ParsedResumeData>(jsonText, options);

                if (parsed != null)
                {
                    parsed.RawText         = fallback.RawText;
                    parsed.UsedAI          = true;
                    parsed.ConfidenceScore = 0.95;
                    return parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI structured parsing failed across all fallback providers");
                fallback.ExtractionIssues.Add($"AI parse error: {ex.Message}");
            }

            return fallback;
        }

        private async Task<string> GenerateTextAsync(string prompt, float temperature, bool jsonMode)
        {
            var errors = new List<string>();

            var order = new List<string> { "OpenAI", "Gemini", "OpenRouter", "Local" };

            foreach (var provider in order)
            {
                try
                {
                    var result = await TryProviderAsync(provider, prompt, temperature, jsonMode);
                    if (!string.IsNullOrWhiteSpace(result)) return result;
                    errors.Add($"{provider}: returned empty response");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AI provider {Provider} failed: {Error}. Trying next fallback.", provider, ex.Message);
                    errors.Add($"{provider}: {ex.Message}");
                }
            }

            throw new Exception($"All AI providers failed. Details: {string.Join(" | ", errors)}");
        }

        private async Task<string> TryProviderAsync(string provider, string prompt, float temperature, bool jsonMode)
        {
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return LocalAIService.GenerateText(prompt, jsonMode);
            }

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey  = _config["AISettings:OpenAI:ApiKey"];
                var baseUrl = _config["AISettings:OpenAI:BaseUrl"] ?? "https://rkapi.com/v1";
                var model   = _config["AISettings:OpenAI:Model"] ?? "gpt-5.5";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenAI ApiKey is missing.");

                var requestBody = new
                {
                    model    = model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenAI HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                {
                    throw new Exception($"OpenAI response is missing 'choices'. Full response: {body}");
                }

                var text = choicesEl[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return jsonMode ? StripMarkdownFences(text) : text;
            }

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("Gemini ApiKey is missing.");

                var client = new Client(apiKey: apiKey);
                var response = await client.Models.GenerateContentAsync(
                    model: _config["AISettings:Gemini:Model"] ?? ParseModel,
                    contents: prompt,
                    config: new GenerateContentConfig
                    {
                        Temperature = temperature,
                        ResponseMimeType = jsonMode ? "application/json" : "text/plain"
                    }
                );

                if (response?.Candidates == null || response.Candidates.Count == 0)
                    throw new Exception("Gemini returned no candidates.");

                var text = response.Candidates[0].Content?.Parts?[0]?.Text ?? "";
                return jsonMode ? StripMarkdownFences(text) : text;
            }
            else if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:OpenRouter:ApiKey"];
                var model  = _config["AISettings:OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenRouter ApiKey is missing.");

                var requestBody = new
                {
                    model    = model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature,
                    response_format = jsonMode ? new { type = "json_object" } : (object?)null
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Headers.Add("HTTP-Referer", "http://localhost:5000");
                request.Headers.Add("X-Title", "AIBasedResumeScreeningSystem");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenRouter HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    var errMsg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    throw new Exception($"OpenRouter API error: {errMsg}");
                }

                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                {
                    throw new Exception($"OpenRouter response is missing 'choices'. Full response: {body}");
                }

                var text = choicesEl[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                return jsonMode ? StripMarkdownFences(text) : text;
            }


            throw new Exception($"Unknown provider: {provider}");
        }

        // ── Utilities ────────────────────────────────────────────────

        private static string TruncateText(string text, int max)
            => string.IsNullOrEmpty(text) || text.Length <= max ? text : text[..max];

        private static string StripMarkdownFences(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text[7..];
            else if (text.StartsWith("```"))
                text = text[3..];
            if (text.EndsWith("```"))
                text = text[..^3];
            return text.Trim();
        }
    }
}
