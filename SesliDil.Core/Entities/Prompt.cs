using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Prompt
    {
        public string PromptId { get; set; } 
        public string AgentId { get; set; }   
        public string Title { get; set; }
        public string Content { get; set; }
        public string? PromptMessage { get; set; }
        public AIAgent Agent { get; set; }
    }
}
