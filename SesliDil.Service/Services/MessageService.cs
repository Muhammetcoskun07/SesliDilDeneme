using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Service.Services
{
    public class MessageService : Service<Message>, IMessageService
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly IMapper _mapper;

        public MessageService(IRepository<Message> messageRepository, IMapper mapper)
            : base(messageRepository, mapper)
        {
            _messageRepository = messageRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MessageDto>> GetByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException("Invalid conversationId", nameof(conversationId));

            var messages = await _messageRepository.GetAllAsync();
            var filtered = messages.Where(m => m.ConversationId == conversationId);
            return _mapper.Map<IEnumerable<MessageDto>>(filtered);
        }
    }
}
