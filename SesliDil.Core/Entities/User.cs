using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class User
    {
        public string? UserId { get; set; } // UUID
        public string? SocialProvider { get; set; } // ENUM: google, apple
        public string SocialId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? NativeLanguage { get; set; } // ISO 639-1
        public string? TargetLanguage { get; set; } // ISO 639-1
        public JsonDocument? LearningGoals { get; set; } // JSON array
        public string? ProficiencyLevel { get; set; } // ENUM: A1, A2, B1, B2, C1, C2
        public string? AgeRange { get; set; } // ENUM: 13-17, 18-24, 25-34, 35-44, 45-54, 55+
        public JsonDocument? Hobbies { get; set; } // JSON array
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public ICollection<Progress> Progresses { get; set; }

    }
}
