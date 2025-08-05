using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class ConversationValidator:AbstractValidator<ConversationDto>
    {
        public ConversationValidator() 
        {
            //RuleFor(x => x.ConversationId).Must(BeValidId).WithMessage("ConversationId must be unique");
           // RuleFor(x => x.CreatedAt).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("CreatedAt cannot be in the future.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("Conversation title cannot be over 100 characters.");
           // RuleFor(x => x.Status).NotEmpty().WithMessage("Status is required.")
             //  .Must(BeValidStatus).WithMessage("Status must be 'Active' or 'Completed'.");
        }
        private bool BeValidId(string conversationId)
        {
            return Guid.TryParse(conversationId, out _);
        }
        private bool BeValidStatus(string conversationStatus)
        {
            return conversationStatus == "Active" || conversationStatus == "Completed";
        }
    }
}
