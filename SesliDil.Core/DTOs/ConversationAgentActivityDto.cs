using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class ConversationAgentActivityDto
    {
        public string ActivityId { get; set; }
        public string ConversationId { get; set; }
        public string UserId { get; set; }
        public string AgentId { get; set; }
        public double DurationMinutes { get; set; }
        public int MessageCount { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
