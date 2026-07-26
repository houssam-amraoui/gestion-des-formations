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
    [Required] public string BasePath { get; set; } = "App_Data/Certificates";
}
