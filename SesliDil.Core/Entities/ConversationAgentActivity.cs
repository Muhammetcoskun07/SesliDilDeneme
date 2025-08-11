using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class ConversationAgentActivity
    {
        public string ActivityId { get; set; } // VARCHAR(50)
        public string ConversationId { get; set; }

        public string UserId { get; set; }  // Kullanıcı ID eklendi

        public string AgentId { get; set; }
        public TimeSpan Duration { get; set; }
        public int WordCount { get; set; }
        public double WordsPerMinute { get; set; }

        public int MessageCount { get; set; }
        public virtual Conversation Conversation { get; set; }
        public virtual User User { get; set; }
        public virtual AIAgent AIAgent { get; set; }
    }
}
