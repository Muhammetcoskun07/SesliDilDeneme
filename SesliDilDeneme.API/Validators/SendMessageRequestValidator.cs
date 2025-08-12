using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required");

            RuleFor(x => x.AgentId)
                .NotEmpty().WithMessage("AgentId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("ConversationId is required");
        }
    }
   
}
