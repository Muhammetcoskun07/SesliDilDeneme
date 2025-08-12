using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class SendMessageRequest
    {
        public string AgentId { get; set; }
        public string Content { get; set; }
        public string ConversationId { get; set; }
        public string UserId { get; set; }
        public string? AudioUrl { get; set; } = null;

    }
}
