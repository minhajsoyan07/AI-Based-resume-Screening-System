using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tesseract;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Interfaces;
using Google.GenAI;
using Google.GenAI.Types;
using IO = System.IO; // disambiguate from Google.GenAI.Types.File

namespace AIResumeScreeningSystem.Services
{
    /// <summary>
    /// Primary AI service for resume analysis and job matching.
    /// Uses the official Google.GenAI SDK. Reads GOOGLE_API_KEY from environment automatically.
    /// </summary>
    public class GeminiResumeService : IAIResumeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiResumeService> _logger;
        private readonly string _tessDataPath;

        // Model constants — easy to update in one place
        private const string TextModel  = "gemini-2.0-flash";
        private const string VisionModel = "gemini-2.0-flash"; // same model, supports vision

        public GeminiResumeService(HttpClient httpClient, IConfiguration config, ILogger<GeminiResumeService> logger)
        {
            _httpClient  = httpClient;
            _config      = config;
            _logger      = logger;
            _tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
        }

        // ══════════════════════════════════════════════════════════════
        // 1. DOCUMENT TEXT EXTRACTION  (PDF / DOCX / Images)
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Extracts raw text from an uploaded file. 
        /// PDF: PdfPig → Tesseract OCR → Gemini Vision
        /// DOCX: OpenXML with table support
        /// Image: Gemini Vision
        /// </summary>
        public async Task<string> ParseDocumentAsync(IFormFile uploadedFile)
        {
            var ext = IO.Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();
            using var stream = uploadedFile.OpenReadStream();

            string raw = ext switch
            {
                ".pdf"  => await ExtractFromPdfAsync(stream, uploadedFile.FileName),
                ".docx" => ExtractFromDocx(stream),
                ".txt"  => await new System.IO.StreamReader(stream).ReadToEndAsync(),
                ".jpg" or ".jpeg" or ".png" =>
                    await ExtractViaGeminiVisionAsync(uploadedFile),
                _ => throw new NotSupportedException($"Unsupported file format: {ext}")
            };

            return SanitizeText(raw);
        }

        public async Task<string> ParseDocumentByPathAsync(string physicalFilePath)
        {
            if (!IO.File.Exists(physicalFilePath))
                throw new FileNotFoundException("Resume file not found", physicalFilePath);

            var ext = IO.Path.GetExtension(physicalFilePath).ToLowerInvariant();
            using var stream = IO.File.OpenRead(physicalFilePath);

            string raw = ext switch
            {
                ".pdf"  => await ExtractFromPdfAsync(stream, physicalFilePath),
                ".docx" => ExtractFromDocx(stream),
                ".txt"  => await IO.File.ReadAllTextAsync(physicalFilePath),
                ".jpg" or ".jpeg" or ".png" =>
                    await ExtractViaGeminiVisionAsync(IO.File.ReadAllBytes(physicalFilePath), physicalFilePath),
                _ => throw new NotSupportedException($"Unsupported file format: {ext}")
            };

            return SanitizeText(raw);
        }

        // ── PDF: PdfPig → Tesseract → Gemini Vision ──────────────────
        private async Task<string> ExtractFromPdfAsync(Stream pdfStream, string fileName)
        {
            var sb = new StringBuilder();

            using var ms = new MemoryStream();
            await pdfStream.CopyToAsync(ms);
            ms.Position = 0;

            byte[] pdfBytes = ms.ToArray();

            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                bool anyPageNeedsOcr = false;

                foreach (var page in doc.GetPages())
                {
                    var text = ContentOrderTextExtractor.GetText(page);
                    if (!string.IsNullOrWhiteSpace(text) && text.Length >= 20 && IsTextReadable(text))
                    {
                        sb.AppendLine(text);
                        continue;
                    }

                    // Layer 2: Tesseract OCR on embedded images
                    bool ocrDone = false;
                    if (Directory.Exists(_tessDataPath) &&
                        IO.File.Exists(IO.Path.Combine(_tessDataPath, "eng.traineddata")))
                    {
                        foreach (var img in page.GetImages())
                        {
                            try
                            {
                                var ocrText = TesseractOcr(img.RawBytes.ToArray());
                                if (!string.IsNullOrWhiteSpace(ocrText) && IsTextReadable(ocrText))
                                {
                                    sb.AppendLine(ocrText);
                                    ocrDone = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Tesseract image OCR failed: {Msg}", ex.Message);
                            }
                        }
                    }

                    if (!ocrDone)
                    {
                        anyPageNeedsOcr = true;
                    }
                }

                // Layer 3: Gemini/Vision fallback (only if some pages were unreadable and not OCRed)
                if (anyPageNeedsOcr)
                {
                    _logger.LogInformation("Falling back to Vision APIs for complete document text extraction.");
                    var visionText = await ExtractViaGeminiVisionAsync(pdfBytes, fileName);
                    if (!string.IsNullOrWhiteSpace(visionText))
                    {
                        // To avoid massive duplication, we only use visionText if sb is very short
                        if (sb.Length < 100)
                            return visionText;
                        else
                            sb.AppendLine("\n--- Additional OCR Data ---\n" + visionText);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF parsing failed — attempting full Vision fallback");
                return await ExtractViaGeminiVisionAsync(pdfBytes, fileName);
            }

            return sb.ToString();
        }

        // ── DOCX: OpenXML with table support ─────────────────────────
        private static string ExtractFromDocx(Stream docxStream)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(docxStream, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var element in body.Elements())
            {
                if (element is Paragraph p)
                    sb.AppendLine(p.InnerText);
                else if (element is Table t)
                    foreach (var row in t.Elements<TableRow>())
                        sb.AppendLine(string.Join(" | ",
                            row.Elements<TableCell>().Select(c => c.InnerText.Trim())));
            }
            return sb.ToString();
        }

        // ── Tesseract helper ──────────────────────────────────────────
        private string TesseractOcr(byte[] imageBytes)
        {
            try
            {
                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                using var pix    = Pix.LoadFromMemory(imageBytes);
                using var page   = engine.Process(pix);
                return page.GetText();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Tesseract OCR error: {Msg}", ex.Message);
                return string.Empty;
            }
        }

        // ── Gemini Vision: extracts text from any file ────────────────
        private async Task<string> ExtractViaGeminiVisionAsync(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);
            return await ExtractViaGeminiVisionAsync(ms.ToArray(), file.FileName);
        }

        private async Task<string> ExtractViaGeminiVisionAsync(byte[] bytes, string fileName)
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
                if (Directory.Exists(_tessDataPath) && IO.File.Exists(IO.Path.Combine(_tessDataPath, "eng.traineddata")))
                {
                    try
                    {
                        var ocrText = TesseractOcr(bytes);
                        if (!string.IsNullOrWhiteSpace(ocrText)) return ocrText;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Local Tesseract vision OCR failed: {Msg}", ex.Message);
                    }
                }
                return "Extracted document text placeholder (Offline Mode).";
            }

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey  = _config["AISettings:OpenAI:ApiKey"];
                var baseUrl = _config["AISettings:OpenAI:BaseUrl"] ?? "http://rkapi.com/v1";
                var model   = _config["AISettings:OpenAI:Model"] ?? "gpt-4.1-mini";

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
                    model,
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
                    throw new Exception($"OpenAI HTTP {(int)response.StatusCode}: {body[..Math.Min(400, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    var errMsg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    throw new Exception($"OpenAI API error: {errMsg}");
                }

                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                    throw new Exception($"OpenAI response is missing 'choices'. Full response: {body}");

                return choicesEl[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            }

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = _config["AISettings:Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("Gemini ApiKey is missing.");

                var client = new Client(apiKey: apiKey);
                var response = await client.Models.GenerateContentAsync(
                    model: _config["AISettings:Gemini:Model"] ?? VisionModel,
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

        // ── Text readability checker ──────────────────────────────────
        private static bool IsTextReadable(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            int totalCount = 0;
            int letterCount = 0;

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                totalCount++;
                if (char.IsLetter(c))
                {
                    letterCount++;
                }
            }

            if (totalCount == 0) return false;

            double ratio = (double)letterCount / totalCount;
            return ratio >= 0.35;
        }

        // ── Text sanitizer ────────────────────────────────────────────
        private static string SanitizeText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = Regex.Replace(raw, @"[ \t]+", " ");
            raw = Regex.Replace(raw, @"(\r\n|\n|\r){2,}", "\n");
            raw = Regex.Replace(raw, @"(?<=\s)[~|^`\\](?=\s)", string.Empty);
            return raw.Trim();
        }

        // ══════════════════════════════════════════════════════════════
        // 2. HYBRID DETERMINISTIC MATCHING ENGINE
        //    Phase 1: AI extracts structured data (skills, exp, edu)
        //    Phase 2: Algorithm computes score deterministically
        // ══════════════════════════════════════════════════════════════

        // ── Score Weights (must sum to 100) ──
        private const double W_SKILL = 40.0;
        private const double W_EXPERIENCE = 20.0;
        private const double W_EDUCATION = 12.0;
        private const double W_KEYWORD = 15.0;
        private const double W_CERTIFICATION = 8.0;
        private const double W_CAREER = 5.0;

        // ── Input hash → result cache (same input = same output) ──
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ResumeMatchResult> _matchCache = new();

        // ── Technology synonym groups for fuzzy skill matching ──
        private static readonly Dictionary<string, HashSet<string>> _synonymGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            {"C#", new(StringComparer.OrdinalIgnoreCase){"CSharp",".NET","DotNet","ASP.NET","ASP.NET Core","Entity Framework","Blazor","LINQ","WPF","WinForms","MAUI"}},
            {"JavaScript", new(StringComparer.OrdinalIgnoreCase){"JS","ES6","ES2015","TypeScript","TS","NodeJS","Node.js","Vanilla JS","ECMAScript"}},
            {"React", new(StringComparer.OrdinalIgnoreCase){"ReactJS","React.js","Redux","React Router","JSX","Next.js","Remix","React Native"}},
            {"Angular", new(StringComparer.OrdinalIgnoreCase){"AngularJS","Angular 2","Angular CLI","RxJS","NgRx"}},
            {"Vue", new(StringComparer.OrdinalIgnoreCase){"VueJS","Vue.js","Vuex","Nuxt","Nuxt.js","Vue 3","Pinia"}},
            {"Python", new(StringComparer.OrdinalIgnoreCase){"Py","Django","Flask","FastAPI","Pandas","NumPy","SciPy","PyTorch","TensorFlow","Jupyter","Celery"}},
            {"Java", new(StringComparer.OrdinalIgnoreCase){"Spring","Spring Boot","Spring MVC","Hibernate","Maven","Gradle","Quarkus","JPA","Servlet","Tomcat"}},
            {"SQL", new(StringComparer.OrdinalIgnoreCase){"TSQL","T-SQL","MySQL","PostgreSQL","Postgres","SQL Server","MSSQL","Oracle DB","SQLite","MariaDB","PL/SQL"}},
            {"NoSQL", new(StringComparer.OrdinalIgnoreCase){"MongoDB","Mongo","DynamoDB","Cassandra","CouchDB","Redis","Couchbase","Firebase","Firestore"}},
            {"AWS", new(StringComparer.OrdinalIgnoreCase){"Amazon Web Services","EC2","S3","Lambda","DynamoDB","ECS","EKS","RDS","CloudFormation","SQS","SNS"}},
            {"Azure", new(StringComparer.OrdinalIgnoreCase){"Microsoft Cloud","Entra","App Service","Azure DevOps","Azure Functions","AKS","Cosmos DB"}},
            {"GCP", new(StringComparer.OrdinalIgnoreCase){"Google Cloud","BigQuery","Cloud Run","Cloud Functions","GKE","Pub/Sub","Dataflow"}},
            {"Docker", new(StringComparer.OrdinalIgnoreCase){"Containers","Containerization","docker-compose","Podman","Container Runtime"}},
            {"Kubernetes", new(StringComparer.OrdinalIgnoreCase){"K8s","Helm","kubectl","Minikube","OpenShift","EKS","AKS","GKE"}},
            {"Machine Learning", new(StringComparer.OrdinalIgnoreCase){"ML","Deep Learning","Neural Network","NLP","Computer Vision","AI","LLM","GPT","Transformer","BERT"}},
            {"DevOps", new(StringComparer.OrdinalIgnoreCase){"CI/CD","Jenkins","GitHub Actions","GitLab CI","CircleCI","IaC","GitOps","Terraform","Ansible","Puppet"}},
            {"PHP", new(StringComparer.OrdinalIgnoreCase){"Laravel","Symfony","CodeIgniter","WordPress","Drupal","Magento","Composer"}},
            {"Ruby", new(StringComparer.OrdinalIgnoreCase){"Rails","Ruby on Rails","RoR","Sinatra","Sidekiq"}},
            {"Go", new(StringComparer.OrdinalIgnoreCase){"Golang","Go Lang","Gin","Echo","Fiber"}},
            {"Rust", new(StringComparer.OrdinalIgnoreCase){"Cargo","Tokio","Actix","WebAssembly","WASM"}},
            {"Swift", new(StringComparer.OrdinalIgnoreCase){"SwiftUI","UIKit","Xcode","iOS Development","Cocoa"}},
            {"Kotlin", new(StringComparer.OrdinalIgnoreCase){"Android Development","Jetpack Compose","Ktor","Coroutines"}},
            {"Data Science", new(StringComparer.OrdinalIgnoreCase){"Data Analysis","Data Analytics","Tableau","Power BI","R","SPSS","SAS","Matplotlib","Seaborn"}},
            {"Git", new(StringComparer.OrdinalIgnoreCase){"GitHub","GitLab","Bitbucket","Version Control","SVN","Mercurial"}},
            {"Linux", new(StringComparer.OrdinalIgnoreCase){"Ubuntu","CentOS","RHEL","Debian","Bash","Shell Scripting","Unix","Fedora"}},
            {"Agile", new(StringComparer.OrdinalIgnoreCase){"Scrum","Kanban","Sprint","SAFe","Jira","Confluence","Retrospective"}},
        };

        // ── Education hierarchy ──
        private static readonly Dictionary<string, int> _eduLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            {"High School",1},{"HSC",1},{"GED",1},{"Secondary",1},
            {"Associate",2},{"Diploma",2},{"Foundation",2},{"Certificate",2},
            {"Bachelor",3},{"BSc",3},{"BBA",3},{"B.Eng",3},{"B.Tech",3},{"BE",3},{"BS",3},{"BA",3},{"BCS",3},{"B.Com",3},
            {"Master",4},{"MSc",4},{"MBA",4},{"M.Tech",4},{"MS",4},{"ME",4},{"MEng",4},{"MA",4},{"MCS",4},{"M.Com",4},
            {"PhD",5},{"Doctorate",5},{"MD",5},{"JD",5},{"DBA",5},{"Ph.D",5}
        };

        // ── Known certifications ──
        private static readonly HashSet<string> _certifications = new(StringComparer.OrdinalIgnoreCase)
        {
            "AWS Certified","Azure Certified","Google Cloud Certified","PMP","CISSP","CCNA","CCNP","CCIE",
            "Scrum Master","CSM","CSPO","PSM","CompTIA A+","CompTIA Security+","CompTIA Network+","ITIL",
            "Six Sigma","CPA","CFA","Oracle Certified","Salesforce Certified","Kubernetes","CKA","CKAD",
            "Docker Certified","Terraform","CEH","OSCP","CISM","CRISC","Red Hat Certified","RHCE","RHCSA",
            "CDP","TOGAF","Prince2","SAFe Agilist","PMI-ACP","ISTQB","Certified Ethical Hacker"
        };

        // ── Career growth keywords ──
        private static readonly string[] _growthTerms = {
            "promoted","promotion","lead","leader","senior","principal","staff","manager","director",
            "mentored","mentoring","achieved","awarded","founded","co-founded","architect","head of",
            "vp","vice president","chief","cto","ceo","coo","cfo","executive"
        };

        public async Task<ResumeMatchResult?> AnalyzeResumeAsync(string resumeText, string jobDescription)
        {
            if (string.IsNullOrWhiteSpace(resumeText) || string.IsNullOrWhiteSpace(jobDescription))
                return null;

            // ── Cache: disabled temporarily to clear old rejected results ──
            var inputHash = ComputeHash(resumeText + "|||" + jobDescription);
            // if (_matchCache.TryGetValue(inputHash, out var cached))
            //     return cached;

            // ══════ PHASE 1: AI EXTRACTION & SCORING ══════
            var ai = await ExtractStructuredDataAsync(resumeText, jobDescription);
            if (ai == null) return null;

            bool isInvalid = "invalid".Equals(ai.Status, StringComparison.OrdinalIgnoreCase) || 
                             (ai.Validation != null && (!ai.Validation.IsResumeValid || !ai.Validation.IsJobDescriptionValid));
            bool isRejected = "rejected".Equals(ai.Status, StringComparison.OrdinalIgnoreCase);

            var result = new ResumeMatchResult
            {
                IsValid = !isInvalid,
                ValidationError = isInvalid ? string.Join(" ", ai.Reasons) : string.Empty,
                MatchScore = (int)ai.FinalScore,
                Verdict = isInvalid
                    ? "Invalid Input: " + string.Join(" ", ai.Reasons)
                    : (isRejected
                        ? "Rejected: " + string.Join(" ", ai.Reasons)
                        : "Excellent Match"),
                Recommendation = string.Join("\n", ai.Reasons),
                CoreSkillMatch  = ai.ExtractedData?.CoreSkillMatch ?? new(),
                MissingSkills   = ai.ExtractedData?.MissingSkills  ?? new(),
                Strengths       = ai.ExtractedData?.Strengths       ?? string.Empty,
                Justification   = ai.ExtractedData?.Justification   ?? string.Empty,
                SkillScore      = (int)Math.Round((ai.Scores?.Skills     ?? 0) * 100),
                ExperienceScore = (int)Math.Round((ai.Scores?.Experience ?? 0) * 100),
                EducationScore  = (int)Math.Round((ai.Scores?.Education  ?? 0) * 100),
                KeywordScore    = (int)Math.Round((ai.Scores?.Keywords   ?? 0) * 100),

                // ── Hard requirement details ──
                EducationMet  = ai.HardRequirements?.EducationMet  ?? false,
                ExperienceMet = ai.HardRequirements?.ExperienceMet ?? false,
                SubjectMet    = ai.HardRequirements?.SubjectMet    ?? false,

                RequiredDegree          = ai.HardRequirements?.RequiredDegree          ?? string.Empty,
                CandidateDegree         = ai.HardRequirements?.CandidateDegree         ?? string.Empty,
                RequiredExperienceYears = ai.HardRequirements?.RequiredExperienceYears  ?? 0,
                CandidateExperienceYears= ai.HardRequirements?.CandidateExperienceYears ?? 0,
                RequiredSubject         = ai.HardRequirements?.RequiredSubject          ?? string.Empty,
                CandidateSubject        = ai.HardRequirements?.CandidateSubject         ?? string.Empty,

                AIProvider = ai.SucceededProvider,
                AIModel = ai.SucceededModel
            };

            // Cache result
            _matchCache.TryAdd(inputHash, result);
            return result;
        }


        private async Task<AIResumeEvaluationResult?> ExtractStructuredDataAsync(string resumeText, string jobDescription)
        {
            var prompt = $@"You are a STRICT AI Resume Screening Engine. Your only job is to evaluate a resume against a job description. Follow every rule below with zero tolerance.

═══════════════════════════════════════════
SECTION A — DOCUMENT VALIDATION (Do first)
═══════════════════════════════════════════
A1. Evaluate if the provided RESUME is a logical professional profile. It MUST contain at least two of: Work Experience, Education, Technical Skills, or a Professional Summary. If the text is 'Lorem Ipsum', placeholder text, a recipe, a news article, fictional prose, or gibberish, set is_resume_valid = false.
A2. Evaluate if the provided JOB DESCRIPTION is a logical job posting. It MUST define a job role, responsibilities, or hiring requirements. If the text is unrelated to employment (e.g., a shopping list, an essay, or generic filler), set is_job_description_valid = false.
A3. REJECTION CRITERIA: If the text is extremely short (less than 100 characters) and lacks context, or if the content is clearly non-professional, trigger a validation failure.
A4. If is_resume_valid is false or is_job_description_valid is false:
  - set status = ""invalid""
  - set final_score = 0
  - In reasons[], list a clear, professional message describing the validation failure (e.g., ""The uploaded document does not appear to be a valid resume or CV. Please upload a profile containing work experience, skills, and education."" or ""The provided job description does not contain valid job details or requirements."")

═══════════════════════════════════════════════════
SECTION B — EXTRACT REQUIREMENTS (If A passes)
═══════════════════════════════════════════════════
From the JOB DESCRIPTION, extract:
  B1. required_degree       : Degree level required. Map to one of: [None, High School, Diploma, Bachelor, Master, PhD]. Use ""None"" only if the JD explicitly says degree is not required.
  B2. required_subject      : Field of study required (e.g. ""Computer Science"", ""Electrical Engineering"", ""Finance""). Use ""Any"" only if the JD explicitly accepts any subject.
  B3. required_experience_years : Minimum years of professional experience required (integer). Use 0 only if the JD says no experience required.

From the RESUME, extract:
  B4. candidate_degree       : Highest degree obtained. Map to the same scale: [None, High School, Diploma, Bachelor, Master, PhD].
  B5. candidate_subject      : Candidate's field of study / major.
  B6. candidate_experience_years : Total professional work experience in years (integer). Count only actual employed work, not internships or education.

DEGREE HIERARCHY (for comparison):
  None=0, High School=1, Diploma=2, Bachelor=3, Master=4, PhD=5

═════════════════════════════════════════════════════════════
SECTION C — HARD REQUIREMENT CHECK (All must PASS to continue)
═════════════════════════════════════════════════════════════
Perform these checks with STRICT logic:

  C1. EDUCATION CHECK
    - Convert required_degree and candidate_degree to their hierarchy number.
    - IF candidate's number < required's number → education_met = false → FAIL
    - IF candidate's number >= required's number → education_met = true → PASS
    - Example: Job requires Bachelor (3), Candidate has Diploma (2) → FAIL

  C2. SUBJECT / MAJOR CHECK
    - IF required_subject == ""Any"" OR required_subject == ""None"" OR required_subject == """" → subject_met = true (no restriction)
    - ELSE: check if the candidate's subject is the same field or a closely related field.
    - Use professional judgment: ""Software Engineering"" matches ""Computer Science"". ""Business Administration"" does NOT match ""Computer Science"".
    - If candidate_subject is empty or unrelated → subject_met = false → FAIL

  C3. EXPERIENCE CHECK
    - IF required_experience_years == 0 → experience_met = true (no restriction)
    - ELSE: IF candidate_experience_years < required_experience_years → experience_met = false → FAIL
    - IF candidate_experience_years >= required_experience_years → experience_met = true → PASS
    - Example: Job requires 3 years, Candidate has 1 year → FAIL

  C4. HARD FAILURE RULE
    - If ANY of C1, C2, C3 is FAIL:
      → final_score MUST be exactly 0
      → status MUST be ""rejected""
      → Do NOT compute soft scores (leave all scores as 0)
      → List ALL failed checks in reasons[] with specific data (e.g. ""Required Bachelor's in CS, candidate holds a Diploma in Business Administration"")

═══════════════════════════════════════════════════════════
SECTION D — SOFT SCORING (ONLY if C1 + C2 + C3 all PASS)
═══════════════════════════════════════════════════════════
Compute these scores between 0.0 and 1.0:

  D1. education   (weight 25%) : 1.0 if candidate exceeds requirement, proportionally less if barely meets
  D2. experience  (weight 30%) : candidate_years / required_years, capped at 1.0. If required=0, give 1.0.
  D3. skills      (weight 35%) : fraction of job-required skills found in the resume
  D4. keywords    (weight 10%) : fraction of meaningful job description keywords found in the resume

  final_score = (education * 0.25 + experience * 0.30 + skills * 0.35 + keywords * 0.10) * 100
  Round to nearest integer. Cap at 100.

  Strictness: Do NOT give a passing score (>=50) unless the candidate genuinely matches the majority of the job's core requirements.

═══════════════════════════
SECTION E — DOCUMENTS
═══════════════════════════
--- JOB DESCRIPTION ---
{jobDescription}

--- RESUME ---
{resumeText}

═══════════════════════════════════════════
SECTION F — OUTPUT (Return ONLY valid JSON)
═══════════════════════════════════════════
{{
  ""final_score"": <integer 0-100>,
  ""status"": ""accepted"" or ""rejected"",
  ""reasons"": [""<Specific sentence with exact data. E.g.: The job requires a Bachelor in Computer Science. The candidate holds a High School certificate, which does not meet the degree requirement.>""],
  ""validation"": {{
    ""is_resume_valid"": <true/false>,
    ""is_job_description_valid"": <true/false>
  }},
  ""hard_requirements"": {{
    ""education_met"": <true/false>,
    ""experience_met"": <true/false>,
    ""subject_met"": <true/false>,
    ""required_degree"": ""<e.g. Bachelor>"",
    ""candidate_degree"": ""<e.g. High School>"",
    ""required_experience_years"": <integer>,
    ""candidate_experience_years"": <integer>,
    ""required_subject"": ""<e.g. Computer Science>"",
    ""candidate_subject"": ""<e.g. Business Administration>""
  }},
  ""scores"": {{
    ""education"": <0.0-1.0>,
    ""experience"": <0.0-1.0>,
    ""skills"": <0.0-1.0>,
    ""keywords"": <0.0-1.0>
  }},
  ""extracted_data"": {{
    ""resume_skills"": [""<skill>""],
    ""job_skills"": [""<skill>""],
    ""core_skill_match"": [""<matched skill>""],
    ""missing_skills"": [""<missing skill>""],
    ""strengths"": ""<Professional, empathetic paragraph about candidate strengths. If the input was invalid, return 'N/A'.>"",
    ""justification"": ""<Detailed career-coach style narrative. If rejected, clearly state which hard requirement failed and why, with the exact extracted values. Offer actionable advice. If accepted, explain why they are a strong match.>""
  }}
}}

ABSOLUTE RULES — NEVER VIOLATE:
- If ANY hard check fails → final_score MUST be 0, status MUST be ""rejected"".
- Never assign a score > 0 to a rejected candidate.
- Never hallucinate data. If a field is not present in the document, state it is missing and treat as not meeting the requirement.
- Output must be a single valid JSON object. No markdown, no explanation outside JSON.";


            string? rawJson = null;
            string succeededProvider = "Local";
            string succeededModel = "Local Heuristic Matcher";
            try
            {
                var textResult = await GenerateTextAsync(prompt, 0.1f, true);
                rawJson = textResult.Text;
                succeededProvider = textResult.Provider;
                succeededModel = textResult.Model;
                
                if (string.IsNullOrWhiteSpace(rawJson)) 
                {
                    throw new Exception("AI API returned null or empty text.");
                }

                var cleanedJson = StripMarkdownFences(rawJson);
                
                // ── JSON Repair Logic: If the model returned extra text, find the JSON block ──
                if (!cleanedJson.Trim().StartsWith("{"))
                {
                    var match = Regex.Match(cleanedJson, @"\{.*\}", RegexOptions.Singleline);
                    if (match.Success) cleanedJson = match.Value;
                }

                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };
                var evalResult = JsonSerializer.Deserialize<AIResumeEvaluationResult>(cleanedJson, options);
                if (evalResult != null)
                {
                    evalResult.SucceededProvider = succeededProvider;
                    evalResult.SucceededModel = succeededModel;
                }
                return evalResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extraction phase failed. Raw JSON: {Raw}", rawJson ?? "null");
                
                // Surface the actual error for easier debugging
                string detailedError = ex.Message;
                if (ex.InnerException != null) detailedError += " -> " + ex.InnerException.Message;
                
                _logger.LogError("All AI providers failed. Detailed Error: {DetailedError}", detailedError);

                return new AIResumeEvaluationResult 
                {
                    FinalScore = 0,
                    Status = "rejected",
                    Reasons = new List<string> 
                    { 
                        "The AI screening service is currently unavailable due to high traffic or provider issues. Please try again later.",
                        $"[Diagnostic Info]: {detailedError}"
                    },
                    Validation = new AIValidation { IsResumeValid = false, IsJobDescriptionValid = false },
                    HardRequirements = new AIHardRequirements { EducationMet = false, ExperienceMet = false },
                    Scores = new AIScores { Education = 0f, Experience = 0f, Skills = 0f, Keywords = 0f },
                    ExtractedData = new AIExtractedData 
                    {
                        ResumeSkills = new List<string>(),
                        JobSkills = new List<string>(),
                        CoreSkillMatch = new List<string>(),
                        MissingSkills = new List<string>(),
                        Strengths = "System unavailable.",
                        Justification = "The system attempted to analyze the match using multiple AI providers, but encountered temporary service disruptions. Please review your API limits or try again later."
                    },
                    SucceededProvider = "None / Failed",
                    SucceededModel = "None / Failed"
                };
            }
        }

        private async Task<(string Text, string Provider, string Model)> GenerateTextAsync(string prompt, float temperature, bool jsonMode)
        {
            var errors = new List<string>();

            var order = new List<string> { "OpenAI", "Gemini", "OpenRouter", "Local" };

            foreach (var provider in order)
            {
                try
                {
                    var result = await TryProviderAsync(provider, prompt, temperature, jsonMode);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        var modelName = GetModelNameForProvider(provider);
                        return (result, provider, modelName);
                    }
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

        private string GetModelNameForProvider(string provider)
        {
            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                return _config["AISettings:OpenAI:Model"] ?? "gpt-4.1-mini";
            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                return _config["AISettings:Gemini:Model"] ?? TextModel;

            if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
                return _config["AISettings:OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";
            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
                return "Local Heuristic Matcher";
            return "unknown";
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
                var baseUrl = _config["AISettings:OpenAI:BaseUrl"] ?? "http://rkapi.com/v1";
                var model   = _config["AISettings:OpenAI:Model"] ?? "gpt-4.1-mini";

                if (string.IsNullOrWhiteSpace(apiKey)) throw new Exception("OpenAI ApiKey is missing.");

                string endpoint = baseUrl.TrimEnd('/') + "/chat/completions";

                var requestBody = new
                {
                    model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    }),
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"OpenAI HTTP {(int)response.StatusCode}: {body[..Math.Min(400, body.Length)]}");

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("error", out var errorEl))
                {
                    var errMsg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    throw new Exception($"OpenAI API error: {errMsg}");
                }

                if (!jsonDoc.RootElement.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
                    throw new Exception($"OpenAI response is missing 'choices'. Full response: {body}");

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
                    model: _config["AISettings:Gemini:Model"] ?? TextModel,
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

        // ── SHA256 hash for input deduplication ──
        private static string ComputeHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        // ── Helper: strip ```json ... ``` if model ignores ResponseMimeType ──
        private static string StripMarkdownFences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = text.Trim();
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(7);
            else if (text.StartsWith("```"))
                text = text.Substring(3);
            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);
            return text.Trim();
        }
    }
}
