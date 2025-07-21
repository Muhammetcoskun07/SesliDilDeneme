using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class FileStorage
    {
        public string FileId { get; set; } // UUID
        public string UserId { get; set; } // UUID, foreign key to User
        public string ConversationId { get; set; } // UUID, foreign key to Conversation
        public string FileName { get; set; }
        public string FileURL { get; set; }
        public DateTime UploadDate { get; set; }
        public User User { get; set; }
        public Conversation Conversation { get; set; }
    }

}
