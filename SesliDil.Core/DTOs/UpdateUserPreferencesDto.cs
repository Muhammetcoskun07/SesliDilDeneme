using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UpdateUserPreferencesDto
    {
        public JsonDocument? LearningGoals { get; set; }
        public JsonDocument? ImprovementGoals { get; set; }
        public JsonDocument? TopicInterests { get; set; }
        public string? WeeklySpeakingGoal { get; set; }
    }
}
