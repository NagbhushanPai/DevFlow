using DevFlow.Application.Projects.DTOs;
using DevFlow.Domain.Entities;
using Mapster;

namespace DevFlow.Application.Common.Mappings;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<Project, ProjectSummaryDto>.NewConfig();

        TypeAdapterConfig<Project, ProjectDto>.NewConfig();
    }
}