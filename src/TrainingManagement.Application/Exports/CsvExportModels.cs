namespace TrainingManagement.Application.Exports;

public sealed record CsvFileResult(string FileName, byte[] Content);

public interface ICsvExportService
{
    byte[] Create(IEnumerable<IReadOnlyCollection<string?>> rows);
    Task<CsvFileResult> ExportAdminEnrollmentsAsync(CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportAdminProgressAsync(CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportAdminAssessmentResultsAsync(CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportAdminCertificatesAsync(CancellationToken cancellationToken = default);
    Task<CsvFileResult?> ExportTrainingAnalyticsAsync(int trainingId, string? trainerId = null, CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportTrainerLearnersAsync(string trainerId, CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportTrainerProgressAsync(string trainerId, CancellationToken cancellationToken = default);
    Task<CsvFileResult> ExportTrainerAssessmentResultsAsync(string trainerId, CancellationToken cancellationToken = default);
}
