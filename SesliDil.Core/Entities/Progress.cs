using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Progress
    {
        public string ProgressId { get; set; } // UUID
        public string UserId { get; set; } // UUID, foreign key to User
        public int DailyConversationCount { get; set; }
        public int TotalConversationTimeMinutes { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public string CurrentLevel { get; set; } // ENUM: A1, A2, B1, B2, C1, C2
        public DateTime LastConversationDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        //public decimal ProgressRate { get; set; }

        public User User { get; set; }

    }
}
