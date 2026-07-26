using System.ComponentModel.DataAnnotations;
using TrainingManagement.Application.Certificates;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Web.ViewModels.Certificates;

public sealed class CertificateIndexViewModel
{
    public string? Search { get; init; }
    public CertificateStatus? Status { get; init; }
    [DataType(DataType.Date)] public DateTime? DateFrom { get; init; }
    [DataType(DataType.Date)] public DateTime? DateTo { get; init; }
    public required TrainingManagement.Application.Common.PagedResult<CertificateListItem> Results { get; init; }
}
public sealed class CertificateRevokeViewModel
{
    public int Id { get; init; }
    public string CertificateNumber { get; init; } = string.Empty;
    [Required(ErrorMessage = "La raison est obligatoire."), MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
public sealed class CertificateVerifyViewModel
{
    [Required(ErrorMessage = "Saisissez un numéro ou un code."), MaxLength(100)]
    public string Query { get; set; } = string.Empty;
    public CertificateVerificationModel? Result { get; init; }
}
