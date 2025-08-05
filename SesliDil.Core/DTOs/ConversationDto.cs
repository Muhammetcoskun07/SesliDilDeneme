using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class ConversationDto
    {
        public string ConversationId { get; set; }
        public string UserId { get; set; }
        public string AgentId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } // active, completed, paused, abandoned
        public string Language { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? DurationMinutes { get; set; }
    }

}
