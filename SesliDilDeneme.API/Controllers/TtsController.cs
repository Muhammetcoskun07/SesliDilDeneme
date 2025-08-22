using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;

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
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                throw new ArgumentException("Text zorunludur.");

            var voice = "alloy"; // veya: GetVoiceByLanguage(request.Language) varsa

            var audioBytes = await _ttsService.ConvertTextToSpeechAsync(request.Text, voice);
            var audioUrl = await _ttsService.SaveAudioToFileAsync(audioBytes);

            // objectsiz: sadece veriyi dön → wrapper ApiResponse<T>.Ok yapacak
            return Ok(new { voice, url = audioUrl });
        }
    }
}
