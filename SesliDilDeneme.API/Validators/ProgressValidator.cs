using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class ProgressValidator:AbstractValidator<ProgressDto>
    {
        public ProgressValidator() 
        {
            RuleFor(x => x.DailyConversationCount).GreaterThan(0).WithMessage("Daily conversation count cannot be negative.");
            RuleFor(x=>x.TotalConversationTimeMinutes).GreaterThan(0).WithMessage("Total conversation time cannot be negative.");
            RuleFor(x => x.CurrentStreakDays).GreaterThanOrEqualTo(0).WithMessage("Current streak days cannot be negative.");
            RuleFor(x => x.UpdatedAt).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Last updated cannot be in the future.");
        }
    }
}
