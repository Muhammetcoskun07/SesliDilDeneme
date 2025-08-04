using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class OpenAIChatResponse
    {
        public List<OpenAIChoice> Choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage Message { get; set; }
    }

    public class OpenAIMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
