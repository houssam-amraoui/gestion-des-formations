using TrainingManagement.Application.Common;

namespace TrainingManagement.Application.Categories;

public sealed record CategorySummary(
    int Id, string Name, string Slug, bool IsActive, int TrainingCount, DateTime CreatedAt);

public sealed record CategoryDetails(
    int Id, string Name, string Slug, string? Description, string? ImageUrl,
    bool IsActive, int TrainingCount, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record CategoryInput(
    string Name, string? Slug, string? Description, string? ImageUrl);

public sealed record CategoryOption(int Id, string Name, bool IsActive);

public sealed record CategoryQuery(string? Search, int Page = 1, int PageSize = 10);

public interface ICategoryService
{
    Task<PagedResult<CategorySummary>> GetPagedAsync(CategoryQuery query, CancellationToken cancellationToken = default);
    Task<CategoryDetails?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryOption>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<ServiceResult<int>> CreateAsync(CategoryInput input, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, CategoryInput input, CancellationToken cancellationToken = default);
    Task<ServiceResult> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
