using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConfigApiController : BaseApiController
    {
        private readonly IConfiguration _configuration;

        public ConfigApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Get current AI model and provider settings
        /// </summary>
        [HttpGet("ai-settings")]
        [AllowAnonymous]
        public IActionResult GetAISettings()
        {
            var provider = _configuration["AISettings:Provider"] ?? "OpenAI";
            var model = "unknown-model";

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                model = _configuration["AISettings:Gemini:Model"] ?? "gemini-2.0-flash";
            }
            else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                model = _configuration["AISettings:OpenAI:Model"] ?? "gpt-5.5";
            }
            else if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                model = _configuration["AISettings:OpenRouter:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";
            }
            else if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                model = "Local Heuristic Matcher";
            }

            return Ok(new
            {
                model = model,
                provider = provider,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
