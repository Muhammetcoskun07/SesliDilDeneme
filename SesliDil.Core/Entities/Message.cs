using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Message
    {
        public string MessageId { get; set; } // UUID
        public string ConversationId { get; set; } // UUID, foreign key to Conversation
        public string Role { get; set; } // ENUM: user, ai
        public string Content { get; set; }
        public string? TranslatedContent { get; set; }
        public string? AudioUrl { get; set; }
        public string SpeakerType { get; set; } // ENUM: user, ai
        public DateTime CreatedAt { get; set; }
        public List<string>? GrammarErrors { get; set; }

        public Conversation Conversation { get; set; }
    }

}
