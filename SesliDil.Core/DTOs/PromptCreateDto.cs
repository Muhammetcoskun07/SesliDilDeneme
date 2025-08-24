using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class PromptCreateDto
    {
        public string AgentId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? PromptMessage { get; set; }
    }
}
