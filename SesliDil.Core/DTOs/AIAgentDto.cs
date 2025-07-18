using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class AIAgentDto
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public string AgentPrompt { get; set; }
    }
}
