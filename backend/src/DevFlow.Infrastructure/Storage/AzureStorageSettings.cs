namespace DevFlow.Infrastructure.Storage;

public sealed class AzureStorageSettings
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "devflow-attachments";
}
