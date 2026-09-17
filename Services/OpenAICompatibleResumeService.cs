using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Interfaces;

namespace AIResumeScreeningSystem.Services
{
    public class OpenAICompatibleResumeService : IAIResumeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenAICompatibleResumeService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly IResumeParserService _localParser; // For document parsing if needed

        public OpenAICompatibleResumeService(
            HttpClient httpClient, 
            IConfiguration config, 
            ILogger<OpenAICompatibleResumeService> logger,
            IResumeParserService localParser)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _localParser = localParser;
            
            var provider = config["AISettings:Provider"] ?? "Modal";
            _apiKey = config[$"AISettings:{provider}:ApiKey"] ?? "";
            _baseUrl = config[$"AISettings:{provider}:BaseUrl"] ?? "";
            _model = config[$"AISettings:{provider}:Model"] ?? "";
        }

        public async Task<string> ParseDocumentAsync(IFormFile uploadedFile)
        {
            // Use the local parser to extract text from the file
            return await _localParser.ExtractTextAsync(uploadedFile) ?? "";
        }

        public async Task<string> ParseDocumentByPathAsync(string physicalFilePath)
        {
            // Use the local parser to extract text from the path
            return await _localParser.ExtractTextFromPathAsync(physicalFilePath) ?? "";
        }

        public async Task<ResumeMatchResult?> AnalyzeResumeAsync(string resumeText, string jobDescription)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_baseUrl))
            {
                _logger.LogError("Modal AI configuration missing ApiKey or BaseUrl");
                return null;
            }

            string prompt = $@"
            You are an expert ATS (Applicant Tracking System). 
            Analyze the provided Resume against the Job Description.
            
            Job Description: {jobDescription}
            
            Resume: {resumeText}

            Return a strict JSON object mapping to this schema:
            {{
                ""MatchScore"": (integer between 0-100),
                ""CoreSkillMatch"": [(array of strings)],
                ""MissingSkills"": [(array of strings)],
                ""Strengths"": ""(short string)"",
                ""Recommendation"": ""(short string)""
            }}";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a specialized ATS analyzer. Always return valid JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                response_format = new { type = "json_object" }
            };

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                
                string endpoint = _baseUrl.EndsWith("/") ? _baseUrl + "chat/completions" : _baseUrl + "/chat/completions";
                var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Modal API returned {StatusCode}: {Error}", response.StatusCode, error);
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);
                
                var aiTextResponse = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (aiTextResponse != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<ResumeMatchResult>(aiTextResponse, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modal AI analysis error");
            }

            return null;
        }
    }
}
