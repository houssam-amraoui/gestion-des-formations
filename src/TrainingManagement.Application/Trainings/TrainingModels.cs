using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Enums;

namespace TrainingManagement.Application.Trainings;

public sealed record TrainerOption(string Id, string DisplayName);

public sealed record TrainingInput(
    string Title, string? Slug, string ShortDescription, string Description,
    string? ThumbnailUrl, int CategoryId, string? TrainerId, TrainingLevel Level,
    string Language, int EstimatedDurationHours, decimal Price, bool IsFree,
    bool RequireAllLessonsCompleted = true, bool RequireAllMandatoryAssessmentsPassed = true,
    decimal? MinimumAverageScore = null, bool CertificateEnabled = true,
    int? CertificateValidityMonths = null, string? CertificateTemplateName = null);

public sealed record TrainingSummary(
    int Id, string Title, string Slug, string ShortDescription, string? ThumbnailUrl, string CategoryName,
    string? TrainerName, TrainingLevel Level, int EstimatedDurationHours, decimal Price,
    bool IsFree, TrainingStatus Status, DateTime CreatedAt);

public sealed record TrainingDetails(
    int Id, string Title, string Slug, string ShortDescription, string Description,
    string? ThumbnailUrl, int CategoryId, string CategoryName, string? TrainerId,
    string? TrainerName, TrainingLevel Level, string Language, int EstimatedDurationHours,
    decimal Price, bool IsFree, TrainingStatus Status, DateTime? PublishedAt,
    DateTime CreatedAt, DateTime? UpdatedAt, bool RequireAllLessonsCompleted,
    bool RequireAllMandatoryAssessmentsPassed, decimal? MinimumAverageScore,
    bool CertificateEnabled, int? CertificateValidityMonths, string? CertificateTemplateName);

public sealed record TrainingQuery(
    string? Search = null, int? CategoryId = null, TrainingLevel? Level = null,
    TrainingStatus? Status = null, string? TrainerId = null,
    string Sort = "created_desc", int Page = 1, int PageSize = 10);

public sealed record PublicTrainingQuery(
    string? Search = null, int? CategoryId = null, TrainingLevel? Level = null,
    int Page = 1, int PageSize = 9);

public interface ITrainingService
{
    Task<PagedResult<TrainingSummary>> GetAdminPagedAsync(TrainingQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<TrainingSummary>> GetPublishedPagedAsync(PublicTrainingQuery query, CancellationToken cancellationToken = default);
    Task<TrainingDetails?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TrainingDetails?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TrainerOption>> GetTrainerOptionsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(TrainingInput input, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, TrainingInput input, CancellationToken cancellationToken = default);
    Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default);
}
