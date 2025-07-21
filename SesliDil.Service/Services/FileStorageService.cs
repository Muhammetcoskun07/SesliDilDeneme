using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;
using SesliDil.Core.DTOs;
using SesliDil.Core.Entities;
using SesliDil.Core.Interfaces;
using SesliDil.Service.Interfaces;

namespace SesliDil.Service.Services
{
    public class FileStorageService : Service<FileStorage>, IService<FileStorage>

    {
        private readonly IRepository<FileStorage> _fileRepository;
        private readonly IMapper _mapper;

        public FileStorageService(IRepository<FileStorage> fileRepository, IMapper mapper)
            : base(fileRepository, mapper)
        {
            _fileRepository = fileRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FileStorageDto>> GetByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException("Invalid conversationId", nameof(conversationId));

            var files = await _fileRepository.GetAllAsync();
            var filtered = files.Where(f => f.ConversationId == conversationId);
            return _mapper.Map<IEnumerable<FileStorageDto>>(filtered);
        }

        public async Task<FileStorageDto> CreateFileStorageAsync(string userId, string conversationId, string fileName, string fileUrl)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileUrl))
                throw new ArgumentException("Invalid input parameters");

            var fileStorage = new FileStorage
            {
                FileId = Guid.NewGuid().ToString(),
                UserId = userId,
                ConversationId = conversationId,
                FileName = fileName,
                FileURL = fileUrl,
                UploadDate = DateTime.UtcNow
            };

            await CreateAsync(fileStorage);
            return _mapper.Map<FileStorageDto>(fileStorage);
        }

    }
}
