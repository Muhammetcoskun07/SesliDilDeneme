using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;
using System.Text.Json;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TtsController : ControllerBase
    {
        private readonly TtsService _ttsService;
        private readonly IRepository<User> _userRepository;

        public TtsController(TtsService ttsService, IRepository<User> userRepository)
        {
            _ttsService = ttsService;
            _userRepository = userRepository;
        }

        private string GetVoiceByLanguage(string language) => "alloy";

        // POST: api/tts/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTts([FromBody] JsonElement body)
        {
            // Body: { "userId": "...", "text": "..." }
            if (!body.TryGetProperty("userId", out var userIdEl) || userIdEl.ValueKind != JsonValueKind.String)
                return BadRequest(new { message = "Invalid request data.", error = "userId is required.", data = (object?)null });

            if (!body.TryGetProperty("text", out var textEl) || textEl.ValueKind != JsonValueKind.String)
                return BadRequest(new { message = "Invalid request data.", error = "text is required.", data = (object?)null });

            var userId = userIdEl.GetString()!;
            var text = textEl.GetString()!;
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(text))
                return BadRequest(new { message = "Invalid request data.", error = "userId and text cannot be empty.", data = (object?)null });

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found.", error = "NOT_FOUND", data = (object?)null });

            var voice = GetVoiceByLanguage(user.TargetLanguage ?? "en");

            // TtsService üstünde olası method adlarını sırayla deneriz
            var service = _ttsService;
            var type = service.GetType();

            // 1) Async string/byte[] dönen imzalar
            var mGenerateAsync = type.GetMethod("GenerateAsync");
            var mSynthesizeAsync = type.GetMethod("SynthesizeAsync");
            // 2) Sync string/byte[] dönen imzalar
            var mGenerate = type.GetMethod("Generate");
            var mSynthesize = type.GetMethod("Synthesize");

            object? result = null;

            try
            {
                if (mGenerateAsync != null)
                {
                    var task = (Task)mGenerateAsync.Invoke(service, new object[] { text, voice })!;
                    await task.ConfigureAwait(false);
                    result = GetTaskResult(task);
                }
                else if (mSynthesizeAsync != null)
                {
                    var task = (Task)mSynthesizeAsync.Invoke(service, new object[] { text, voice })!;
                    await task.ConfigureAwait(false);
                    result = GetTaskResult(task);
                }
                else if (mGenerate != null)
                {
                    result = mGenerate.Invoke(service, new object[] { text, voice });
                }
                else if (mSynthesize != null)
                {
                    result = mSynthesize.Invoke(service, new object[] { text, voice });
                }
                else
                {
                    return StatusCode(501, new
                    {
                        message = "TTS method not implemented on TtsService.",
                        error = "NO_TTS_METHOD",
                        data = (object?)null
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "TTS generation failed.", error = ex.Message, data = (object?)null });
            }

            // Sonucu JSON’a uygun hale getir
            object payload;
            switch (result)
            {
                case byte[] bytes:
                    payload = new { format = "base64", value = Convert.ToBase64String(bytes) };
                    break;
                case string s:
                    payload = new { format = "url-or-text", value = s };
                    break;
                default:
                    payload = result ?? new { };
                    break;
            }

            return Ok(new
            {
                message = "TTS generated successfully.",
                error = (string?)null,
                data = new
                {
                    voice,
                    result = payload
                }
            });
        }

        // Task<T> result'unu almak için küçük yardımcı
        private static object? GetTaskResult(Task task)
        {
            var t = task.GetType();
            if (t.IsGenericType && t.GetProperty("Result") != null)
            {
                return t.GetProperty("Result")!.GetValue(task);
            }
            return null;
        }
    }
}
