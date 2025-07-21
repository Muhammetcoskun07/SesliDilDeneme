using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class AIAgentDto
    {
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string AgentPrompt { get; set; }
        public string AgentDescription { get; set; }
        public string AgentType { get; set; } // conversation, travel, business, grammar
        public bool IsActive { get; set; }
    }
}
