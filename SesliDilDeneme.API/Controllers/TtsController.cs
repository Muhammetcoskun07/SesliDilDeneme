using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
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
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateTts([FromBody] TtsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Invalid request data.", error = "text cannot be empty.", data = (object?)null });

            var voice = "alloy";

            try
            {
                var audioBytes = await _ttsService.ConvertTextToSpeechAsync(request.Text, voice);
                var audioUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

                return Ok(new
                {
                    message = "TTS generated successfully.",
                    error = (string?)null,
                    data = new { voice, url = audioUrl }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "TTS generation failed.", error = ex.Message, data = (object?)null });
            }
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
