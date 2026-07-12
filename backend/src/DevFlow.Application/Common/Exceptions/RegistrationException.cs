namespace DevFlow.Application.Common.Exceptions;

public sealed class RegistrationException : Exception
{
    public RegistrationException(IEnumerable<string> errors)
        : base("User registration failed.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyCollection<string> Errors { get; }
}