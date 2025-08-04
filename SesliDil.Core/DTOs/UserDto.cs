using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserDto
    {
        public string UserId { get; set; }
        public string SocialProvider { get; set; }
        public string SocialId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NativeLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string[] LearningGoals { get; set; }
        public string ProficiencyLevel { get; set; }
        public string AgeRange { get; set; }
        public Boolean HasCompletedOnboarding { get; set; }
        public string[] ImprovementGoals { get; set; }
        public string[] TopicInterests { get; set; }
        public string WeeklySpeakingGoal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
    }

}
