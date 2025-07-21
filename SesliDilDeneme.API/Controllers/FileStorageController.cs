using Microsoft.AspNetCore.Mvc;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Service.Services;

namespace SesliDilDeneme.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileStorageController : Controller
    {
        private readonly FileStorageService _fileService;

        public FileStorageController(FileStorageService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var files = await _fileService.GetAllAsync();
            return Ok(files);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var file = await _fileService.GetByIdAsync<string>(id);
            if (file == null) return NotFound();
            return Ok(file);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetByConversationId(string conversationId)
        {
            var files = await _fileService.GetByConversationIdAsync(conversationId);
            return Ok(files);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FileStorage file)
        {
            file.FileId = Guid.NewGuid().ToString();
            file.UploadDate = DateTime.UtcNow;

            await _fileService.CreateAsync(file);
            return CreatedAtAction(nameof(GetById), new { id = file.FileId }, file);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var file = await _fileService.GetByIdAsync<string>(id);
            if (file == null) return NotFound();

            await _fileService.DeleteAsync(file);
            return NoContent();
        }
    }
}
