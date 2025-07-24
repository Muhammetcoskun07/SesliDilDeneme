using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class MessageDto
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public string Role { get; set; } // user, ai
        public string Content { get; set; }
        public string TranslatedContent { get; set; }
        public string AudioUrl { get; set; }
        public string SpeakerType { get; set; } // user, ai
        public DateTime CreatedAt { get; set; }
        public List<string> GrammarErrors { get; set; }
    }

}
