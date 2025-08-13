using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserUpdateDto
    {
        public string TargetLanguage { get; set; }
        public string ProficiencyLevel { get; set; }
        public string[] LearningGoals { get; set; }
        public string[] ImprovementGoals { get; set; }
        public string[] TopicInterests { get; set; }
        public string WeeklySpeakingGoal { get; set; }
    }
}
