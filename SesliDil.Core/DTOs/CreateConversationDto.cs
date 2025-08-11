using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class CreateConversationDto
    {
        public string? UserId { get; set; }
        public string? AgentId { get; set; }
        public string? Title { get; set; }
    }
}
