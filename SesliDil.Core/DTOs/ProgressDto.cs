using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class ProgressDto
    {
        public string ProgressId { get; set; }
        public string UserId { get; set; }
        public int DailyConversationCount { get; set; }
        public int TotalConversationTimeMinutes { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }
        public string CurrentLevel { get; set; } // A1 - C2
        public double BestWordsPerMinute { get; set; } // Best WPM achieved by the user
        public DateTime LastConversationDate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
