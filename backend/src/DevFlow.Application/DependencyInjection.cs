using DevFlow.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DevFlow.Application.Common.Mappings;
using DevFlow.Application.Common.Authorization;
using DevFlow.Application.Organizations;
using DevFlow.Application.Projects;
using DevFlow.Application.Issues;
using DevFlow.Application.Sprints;

namespace DevFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        MappingConfig.RegisterMappings();

        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
        services.AddScoped<IOrganizationManagementService, OrganizationManagementService>();
        services.AddScoped<IProjectManagementService, ProjectManagementService>();
        services.AddScoped<IIssueManagementService, IssueManagementService>();
        services.AddScoped<ISprintManagementService, SprintManagementService>();

        return services;
    }
}
