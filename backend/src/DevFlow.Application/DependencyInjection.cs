using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Common.Behaviors;
using DevFlow.Application.Issues;
using DevFlow.Application.Notifications;
using DevFlow.Application.Organizations;
using DevFlow.Application.Projects;
using DevFlow.Application.Sprints;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DevFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Authentication flows (login/register/refresh) are MediatR commands with FluentValidation;
        // domain features are plain services behind interfaces.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
        services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        services.AddScoped<IOrganizationManagementService, OrganizationManagementService>();
        services.AddScoped<IProjectManagementService, ProjectManagementService>();
        services.AddScoped<IIssueManagementService, IssueManagementService>();
        services.AddScoped<ISprintManagementService, SprintManagementService>();
        services.AddScoped<INotificationManagementService, NotificationManagementService>();
        services.AddScoped<IAttachmentManagementService, AttachmentManagementService>();

        return services;
    }
}
