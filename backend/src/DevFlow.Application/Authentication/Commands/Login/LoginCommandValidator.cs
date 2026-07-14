using FluentValidation;

namespace DevFlow.Application.Authentication.Commands.Login;

public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
