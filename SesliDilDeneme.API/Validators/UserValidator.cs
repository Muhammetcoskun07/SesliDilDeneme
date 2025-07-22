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
            RuleFor(x => x.Hobbies).NotEmpty().WithMessage("Hobbies are required.");
        }
        private bool BeValidLanguage(string language)
        {
            var validLanguages = new[] { "Turkısh", "English", "German", "French", "Spanish" };
            if (validLanguages.Contains(language) && language!=null) return true;
            return false;
        }
        private bool BeValidAgeRange(string ageRange)
        {
            var validageRange = new[] { "18-24", "25-29", "30-35", "36-40", "41+" };
            if (validageRange.Contains(ageRange) && ageRange != null) return true;
            return false;
        }
    }
}
