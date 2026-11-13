using DevFlow.Application.Issues;
using DevFlow.Application.Organizations;
using DevFlow.Application.Projects;
using DevFlow.Application.Sprints;
using DevFlow.Domain.Enums;
using FluentValidation;

namespace DevFlow.API.Controllers;

// Request contracts for the API and their validators. Services re-check invariants, but validating here
// returns all field errors at once as a 400 ValidationProblemDetails.

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);

public sealed record CreateOrganizationRequest(string Name, string? Description);

public sealed record UpdateOrganizationRequest(string Name, string? Description);

public sealed record OrganizationMemberRequest(string Role);

public sealed record CreateTeamRequest(string Name, string? Description);

public sealed record UpdateTeamRequest(string Name, string? Description);

public sealed record TeamMemberRequest(string Role);

public sealed record CreateProjectRequest(Guid OrganizationId, string Name, string Key, string? Description);

public sealed record UpdateProjectRequest(string Name, string Key, string? Description, ProjectStatus Status);

public sealed record ProjectMemberRequest(ProjectMemberRole Role);

public sealed record GitHubRepositoryRequest(string? Owner, string? Name);

public sealed record IssueCreateRequest(string Title, string? Description, IssueType Type, IssuePriority Priority, Guid? AssigneeId);

public sealed record IssueUpdateRequest(string Title, string? Description, IssueType Type, IssueStatus Status, IssuePriority Priority, Guid? AssigneeId);

public sealed record CommentRequest(string Content);

public sealed record SprintRequest(string Name, string? Goal, DateTime? StartDateUtc, DateTime? EndDateUtc);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrganizationManagementService.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(OrganizationManagementService.DescriptionMaxLength);
    }
}

public sealed class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrganizationManagementService.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(OrganizationManagementService.DescriptionMaxLength);
    }
}

public sealed class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrganizationManagementService.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(OrganizationManagementService.DescriptionMaxLength);
    }
}

public sealed class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
{
    public UpdateTeamRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(OrganizationManagementService.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(OrganizationManagementService.DescriptionMaxLength);
    }
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ProjectManagementService.NameMaxLength);
        RuleFor(x => x.Key).NotEmpty().Matches("^[A-Za-z0-9]{2,10}$").WithMessage("Project key must be 2-10 letters or digits.");
        RuleFor(x => x.Description).MaximumLength(ProjectManagementService.DescriptionMaxLength);
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ProjectManagementService.NameMaxLength);
        RuleFor(x => x.Key).NotEmpty().Matches("^[A-Za-z0-9]{2,10}$").WithMessage("Project key must be 2-10 letters or digits.");
        RuleFor(x => x.Description).MaximumLength(ProjectManagementService.DescriptionMaxLength);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class ProjectMemberRequestValidator : AbstractValidator<ProjectMemberRequest>
{
    public ProjectMemberRequestValidator() => RuleFor(x => x.Role).IsInEnum();
}

public sealed class IssueCreateRequestValidator : AbstractValidator<IssueCreateRequest>
{
    public IssueCreateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(IssueManagementService.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(IssueManagementService.DescriptionMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class IssueUpdateRequestValidator : AbstractValidator<IssueUpdateRequest>
{
    public IssueUpdateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(IssueManagementService.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(IssueManagementService.DescriptionMaxLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class CommentRequestValidator : AbstractValidator<CommentRequest>
{
    public CommentRequestValidator() =>
        RuleFor(x => x.Content).NotEmpty().MaximumLength(IssueManagementService.CommentMaxLength);
}

public sealed class SprintRequestValidator : AbstractValidator<SprintRequest>
{
    public SprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(SprintManagementService.NameMaxLength);
        RuleFor(x => x.Goal).MaximumLength(SprintManagementService.GoalMaxLength);
        RuleFor(x => x.EndDateUtc)
            .GreaterThanOrEqualTo(x => x.StartDateUtc)
            .When(x => x.StartDateUtc.HasValue && x.EndDateUtc.HasValue)
            .WithMessage("The end date must be on or after the start date.");
    }
}
