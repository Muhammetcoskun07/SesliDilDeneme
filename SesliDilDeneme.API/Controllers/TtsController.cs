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
    }
}
