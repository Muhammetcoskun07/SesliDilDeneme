using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class UserValidator:AbstractValidator<UserDto>
    {
        public UserValidator()
        {
            RuleFor(x => x.TargetLanguage).NotEmpty().
                WithMessage("Target Language is required").Must(BeValidLanguage).WithMessage("Invalid target Language");
            RuleFor(x => x.AgeRange).NotEmpty().
                WithMessage("AgeRange is required").Must(BeValidAgeRange).WithMessage("AgeRange must be in format '18-24' etc.");
            RuleFor(x => x.LearningGoals).NotEmpty().WithMessage("Learning goals are required.");
            RuleFor(x => x.NativeLanguage).NotEmpty().
                WithMessage("Native Language is required").Must(BeValidLanguage).WithMessage("Invalid native Language");
            RuleFor(x => x.ImprovementGoals)
              .NotEmpty().WithMessage("Improvement Goals are required")
              .Must(goals => goals != null && goals.Any()).WithMessage("At least one improvement goal is required")
              .Must(BeValidImprovementGoals).WithMessage("Invalid improvement goal");

            RuleFor(x => x.TopicInterests)
                .NotEmpty().WithMessage("Topic Interests are required")
                .Must(interests => interests != null && interests.Any()).WithMessage("At least one topic interest is required")
                .Must(BeValidTopicInterests).WithMessage("Invalid topic interest");

            RuleFor(x => x.WeeklySpeakingGoal)
                .NotEmpty().WithMessage("Weekly Speaking Goal is required")
                .Must(BeValidWeeklySpeakingGoal).WithMessage("Invalid weekly speaking goal");

            RuleFor(x => x.LearningGoals)
                .NotEmpty().WithMessage("Learning Goals are required")
                .Must(goals => goals != null && goals.Any()).WithMessage("At least one learning goal is required")
                .Must(BeValidLearningGoals).WithMessage("Invalid learning goal");

        }
        
         private bool BeValidLanguage(string language)
        {
            var validLanguages = new[] { "Turkish","English", "German", "French", "Spanish", "Italian" };
            return language != null && validLanguages.Contains(language);
        }
        private bool BeValidAgeRange(string ageRange)
        {
            var validAgeRanges = new[] { "13-17", "18-29", "30-44", "45-54", "55-64", "65 and over" };
            return ageRange != null && validAgeRanges.Contains(ageRange);
        }
        private bool BeValidProficiencyLevel(string level)
        {
            var validLevels = new[] { "A1  I know basic words and simple phrases", "A2  I can carry on basic conversations",
                "B1  I know basic words and simple phrases", "B2  I can discuss various topics with ease",
                "C1  I speak confidently in complex situations", "C2  I speak like a native in all contexts" };
            return level != null && validLevels.Contains(level);
        }
        private bool BeValidImprovementGoals(string[] goals)
        {
            var validGoals = new[] { "Travelling", "Daily talk", "Career growth", "Academic purposes" };
            return goals != null && goals.All(g => validGoals.Contains(g));
        }
        private bool BeValidTopicInterests(string[] interests)
        {
            var validInterests = new[] { "Travel", "Food", "Music", "Sports", "Technology", "Culture", "Health & Fitness",
                                       "Education", "Work & Business", "Entertainment", "Shopping", "Relationships",
                                       "Art & Design", "Animals", "Environment" };
            return interests != null && interests.All(i => validInterests.Contains(i));
        }
        private bool BeValidWeeklySpeakingGoal(string goal)
        {
            var validGoals = new[] { "5-10 minutes a day", "15-20 minutes a day", "30 minutes a day", "45+ minutes a day" };
            return goal != null && validGoals.Contains(goal);
        }
        private bool BeValidLearningGoals(string[] goals)
        {
            var validGoals = new[] { "Speak better", "Understand better", "Improve grammar", "Sound natural",
                                  "Expand vocabulary", "Handle real-life talks" };
            return goals != null && goals.All(g => validGoals.Contains(g));
        }
    }
}
