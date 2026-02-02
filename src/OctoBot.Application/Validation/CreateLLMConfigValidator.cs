using FluentValidation;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Validation;

public class CreateLLMConfigValidator : AbstractValidator<CreateLLMConfigDto>
{
    public CreateLLMConfigValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.ProviderType)
            .NotEmpty().WithMessage("Provider type is required")
            .MaximumLength(50).WithMessage("Provider type must not exceed 50 characters");

        RuleFor(x => x.ModelId)
            .MaximumLength(100).WithMessage("Model ID must not exceed 100 characters");

        RuleFor(x => x.Endpoint)
            .MaximumLength(500).WithMessage("Endpoint must not exceed 500 characters");
    }
}
