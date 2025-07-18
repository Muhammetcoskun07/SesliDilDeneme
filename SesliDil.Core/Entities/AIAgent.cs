using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class AIAgent
    {
        public string AgentId { get; set; } // VARCHAR(50)
        public string AgentName { get; set; }
        public string AgentPrompt { get; set; }
        public string AgentDescription { get; set; }
        public string AgentType { get; set; } // ENUM: conversation, travel, business, grammar
        public bool IsActive { get; set; } // default true

    }
}
