using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Service.Services
{
    public class FileStorageService : Service<FileStorage>, IFileStorageService
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
    }
}
