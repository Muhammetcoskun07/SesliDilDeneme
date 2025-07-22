using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class AIAgentValidator:AbstractValidator<AIAgentDto>
    {
        public AIAgentValidator()
        {
            RuleFor(x => x.AgentId).NotEmpty().WithMessage("Agent ID is required.")
                .Must(BeValidGuid).WithMessage("Agent ID must be unique.");
            RuleFor(x => x.AgentName).NotEmpty().WithMessage("Agent name is required.")
                .MaximumLength(100).WithMessage("Agent name cannot be over 100 characters.");
            RuleFor(x => x.AgentPrompt).NotEmpty().WithMessage("Agent prompt is required.")
                .MaximumLength(1000).WithMessage("Agent prompt cannot be over characters.");
            RuleFor(x => x.AgentDescription)
                .MaximumLength(500).WithMessage("Agent description cannot be over 500 characters.");
            RuleFor(x => x.AgentType).NotEmpty().WithMessage("Agent type is required.")
                .Must(BeValidAgentType).WithMessage("Agent type must be 'casual talk', 'travel', 'business', or 'grammar'.");
        }
        private bool BeValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
        private bool BeValidAgentType(string agentType)
        {
            var agent = new[] { "casual talk", "travel", "business", "grammar"};
            if (agent.Contains(agentType)) return true;
            return false;
        }
    }
}
