using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ProgressService _progressService;                 // KALSIN (kullanmasak da)
        private readonly IRepository<Progress> _progressRepository;        // KALSIN (kullanmasak da)

        public UserController(
            UserService userService,
            ProgressService progressService,
            IRepository<Progress> progressRepository)
        {
            _userService = userService;
            _progressService = progressService;        // KALSIN
            _progressRepository = progressRepository;  // KALSIN
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userService.GetByIdOrThrowAsync(id);
            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto userDto)
        {
            var user = await _userService.CreateFromDtoAsync(userDto);
            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, user);
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UserUpdateDto userDto)
        {
            var user = await _userService.UpdateFromDtoAsync(id, userDto);
            return Ok(user);
        }

        // POST: api/user/social
        [HttpPost("social")]
        public async Task<IActionResult> CreateOrUpdateBySocial([FromBody] UserDto userDto)
        {
            var user = await _userService.CreateOrUpdateBySocialValidatedAsync(userDto);
            return Ok(user);
        }

        // POST: api/user/onboarding/{userId}
        [HttpPost("onboarding/{userId}")]
        public async Task<IActionResult> Onboarding(string userId, [FromBody] OnboardingDto onboardingDto)
        {
            await _userService.OnboardingAsync(userId, onboardingDto);
            return Ok(new { userId, hasCompletedOnboarding = true });
        }

        // PATCH: api/user/{userId}/preferences
        [HttpPatch("{userId}/preferences")]
        public async Task<IActionResult> UpdatePreferences(string userId, [FromBody] UpdateUserPreferencesDto dto)
        {
            var user = await _userService.UpdatePreferencesAsync(userId, dto);
            return Ok(user);
        }

        // PUT: api/user/update-profile/{userId}
        [HttpPut("update-profile/{userId}")]
        public async Task<IActionResult> UpdateProfile([FromRoute] string userId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var user = await _userService.UpdateProfileAsync(userId, request.FirstName, request.LastName);
                return Ok(new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/user/{id}/full-delete
        [HttpDelete("{id}/full-delete")]
        public async Task<IActionResult> DeleteUserCompletely(string id)
        {
            var result = await _userService.DeleteUserCompletelyAsync(id);
            if (!result)
                throw new KeyNotFoundException();

            return Ok(new { Deleted = true, Purged = true, Id = id });
        }
    }
}
