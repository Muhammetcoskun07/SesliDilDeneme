using FluentValidation;

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

        }
    }
   
}
