using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class ConversationDto
    {
        public string? UserId { get; set; }
        public string? AgentId { get; set; }
        public string? Title { get; set; }
        //public string? Status { get; set; }
        public double? DurationMinutes { get; set; }
    }

}
