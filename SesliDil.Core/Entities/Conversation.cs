using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Conversation
    {
        public string ConversationId { get; set; } // UUID
        public string UserId { get; set; } // UUID, foreign key to User
        public string AgentId { get; set; } // VARCHAR(50)
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Summary { get; set; }
        public string Status { get; set; } // ENUM: active, completed, paused, abandoned
        public string Language { get; set; } // ISO 639-1
        public DateTime StartedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }

        public AIAgent Agent { get; set; }
        public User User { get; set; } // Navigation property
        public ICollection<Message> Messages { get; set; }
        public ICollection<FileStorage> Files { get; set; }
    }

}
