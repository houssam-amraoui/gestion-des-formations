using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Infrastructure.Certificates;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";
    [Required, MaxLength(100)] public string Name { get; set; } = "Training Management";
    [Required, Url] public string PublicBaseUrl { get; set; } = "http://localhost:5012";
}

public sealed class CertificateStorageOptions
{
    public const string SectionName = "CertificateStorage";
    [Required] public string Provider { get; set; } = "Local";
    [Required] public string BasePath { get; set; } = "App_Data/Certificates";
}

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";
    [Required] public string KeysPath { get; set; } = "App_Data/Keys";
    [Required, MaxLength(200)] public string ApplicationName { get; set; } = "TrainingManagement";
}
