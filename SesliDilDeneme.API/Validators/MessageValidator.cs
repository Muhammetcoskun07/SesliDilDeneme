using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class MessageValidator:AbstractValidator<MessageDto>
    {
        public MessageValidator()
        {
            RuleFor(x => x.MessageId).NotEmpty().WithMessage("Message ID is required.")
                .Must(BeValidGuid).WithMessage("Message ID must be unique.");
            RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Conversation ID is required.")
                .Must(BeValidGuid).WithMessage("Conversation ID must be be unique.");
            RuleFor(x => x.Role).NotEmpty().WithMessage("Role is required.")
               .Must(BeValidRole).WithMessage("Role must be 'user' or 'ai'.");
            RuleFor(x => x.SpeakerType).NotEmpty().WithMessage("Speaker type is required.")
                .Must(BeValidSpeakerType).WithMessage("Speaker type must be 'user' or 'ai'.");
            RuleFor(x => x.CreatedAt) .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("CreatedAt cannot be in the future.");
        }
        private bool BeValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
        private bool BeValidRole(string role)
        {
            return new[] { "user", "ai" }.Contains(role?.ToLower());
        }

        private bool BeValidSpeakerType(string speakerType)
        {
            return new[] { "user", "ai" }.Contains(speakerType?.ToLower());
        }
    }
}
