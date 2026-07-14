using FluentValidation;

namespace DevFlow.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator
    : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Key)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(10)
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage(
                "Project key must contain only letters and numbers.");

        RuleFor(x => x.Description)
            .MaximumLength(2000);
    }
}