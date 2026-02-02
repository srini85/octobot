using FluentValidation;
using OctoBot.Application.DTOs;

namespace OctoBot.Application.Validation;

public class CreateBotInstanceValidator : AbstractValidator<CreateBotInstanceDto>
{
    public CreateBotInstanceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.SystemPrompt)
            .MaximumLength(10000).WithMessage("System prompt must not exceed 10000 characters");
    }
}
