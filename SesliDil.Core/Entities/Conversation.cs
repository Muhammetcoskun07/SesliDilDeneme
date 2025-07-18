using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Conversation
    {
        public int ThreadId { get; set; }
        public int AgentId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }



        public AIAgent Agent { get; set; }
        public ICollection<Message> Messages { get; set; }
        public ICollection<FileStorage> Files { get; set; }
    }

}
