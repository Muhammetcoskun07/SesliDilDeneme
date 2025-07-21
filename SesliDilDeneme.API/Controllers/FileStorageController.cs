using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileStorageController : ControllerBase
    {
        private readonly FileStorageService _fileService;

        public FileStorageController(FileStorageService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            var files = await _fileService.GetByConversationIdAsync(conversationId);
            return Ok(files);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFile([FromBody] FileStorageDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var createdFile = await _fileService.CreateFileAsync(dto);
            return Ok(createdFile);
        }
    }
}
