using FluentValidation;
using SesliDil.Core.DTOs;

namespace SesliDilDeneme.API.Validators
{
    public class FileStorageValidator:AbstractValidator<FileStorageDto>
    {
        public FileStorageValidator() 
        {
            RuleFor(x=>x.FileId).NotEmpty().WithMessage("File ID is required.").Must(BeValidGuid).WithMessage("File ID must be unique.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.").Must(BeValidGuid).WithMessage("User ID must be unique.");
            RuleFor(x => x.ConversationId).NotEmpty().WithMessage("Conversation ID is required.") .Must(BeValidGuid).WithMessage("Conversation ID must be unique.");
            RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.").MaximumLength(255).WithMessage("File name cannot be over 255 characters.");
            RuleFor(x => x.FileURL).NotEmpty().WithMessage("File URL is required.");
                //.Must(BeValidUrl).WithMessage("File URL must be a valid URL.");
            RuleFor(x => x.UploadDate).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Upload date cannot be in the future.");
        }
        private bool BeValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
        /*
        private bool BeValidUrl(string url)
        {

        }
        */
    }
}
