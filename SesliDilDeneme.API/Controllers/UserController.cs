using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;
using System.Security.Claims;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid Id");
            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] UserDto userDto)
        {
            if (userDto == null) return BadRequest("Invalid User Data");

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                SocialProvider = userDto.SocialProvider,
                SocialId = userDto.SocialId,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                NativeLanguage = userDto.NativeLanguage,
                TargetLanguage = userDto.TargetLanguage,
                ProficiencyLevel = userDto.ProficiencyLevel,
                AgeRange = userDto.AgeRange,
                LastLoginAt = DateTime.UtcNow
            };

            await _userService.CreateAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserDto userDto)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Invalid id");
            var user = await _userService.GetByIdAsync<string>(id);
            if (user == null) return NotFound();

            user.SocialProvider = userDto.SocialProvider;
            user.SocialId = userDto.SocialId;
            user.Email = userDto.Email;
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.NativeLanguage = userDto.NativeLanguage;
            user.TargetLanguage = userDto.TargetLanguage;
            user.ProficiencyLevel = userDto.ProficiencyLevel;
            user.AgeRange = userDto.AgeRange;
            user.LastLoginAt = DateTime.UtcNow;

            await _userService.UpdateAsync(user);
            return NoContent();
        }

        [HttpPost("social")]
        public async Task<ActionResult<UserDto>> CreateOrUpdateBySocial([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.SocialProvider)) return BadRequest("Invalid data");

            var user = await _userService.GetOrCreateBySocialAsync(
                userDto.SocialProvider,
                userDto.SocialId,
                userDto.Email,
                userDto.FirstName,
                userDto.LastName
            );

            return Ok(user);
        }

        [Authorize]
        [HttpPost("onboarding")]
        public async Task<IActionResult> Onboarding([FromBody] OnboardingDto onboardingDto)
        {
            if (onboardingDto == null) return BadRequest("Invalid onboarding data");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetByIdAsync<string>(userId);
            if (user == null) return NotFound();

            user.NativeLanguage = onboardingDto.NativeLanguage;
            user.TargetLanguage = onboardingDto.TargetLanguage;
            user.ProficiencyLevel = onboardingDto.ProficiencyLevel;
            user.AgeRange = onboardingDto.AgeRange;

            await _userService.UpdateAsync(user);
            return Ok("Onboarding bilgileri kaydedildi.");
        }
    }
}
