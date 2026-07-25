using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Categories;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class CategoryService(ApplicationDbContext dbContext) : ICategoryService
{
    public async Task<PagedResult<CategorySummary>> GetPagedAsync(CategoryQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var categories = dbContext.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            categories = categories.Where(category => category.Name.Contains(search));
        }

        var total = await categories.CountAsync(cancellationToken);
        var items = await categories.OrderBy(category => category.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(category => new CategorySummary(category.Id, category.Name, category.Slug,
                category.IsActive, category.Trainings.Count, category.CreatedAt))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    public Task<CategoryDetails?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.AsNoTracking().Where(category => category.Id == id)
            .Select(category => new CategoryDetails(category.Id, category.Name, category.Slug,
                category.Description, category.ImageUrl, category.IsActive, category.Trainings.Count,
                category.CreatedAt, category.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CategoryOption>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsNoTracking();
        if (activeOnly) query = query.Where(category => category.IsActive);
        return await query.OrderBy(category => category.Name)
            .Select(category => new CategoryOption(category.Id, category.Name, category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<int>> CreateAsync(CategoryInput input, CancellationToken cancellationToken = default)
    {
        var name = input.Name.Trim();
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(input.Slug) ? name : input.Slug);
        var error = await ValidateUniqueAsync(name, slug, null, cancellationToken);
        if (error is not null) return ServiceResult<int>.Failure(error);
        if (string.IsNullOrWhiteSpace(slug)) return ServiceResult<int>.Failure("Le slug est invalide.");

        var category = new Category
        {
            Name = name, Slug = slug, Description = NullIfWhiteSpace(input.Description),
            ImageUrl = NullIfWhiteSpace(input.ImageUrl), CreatedAt = DateTime.UtcNow, IsActive = true
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(category.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int id, CategoryInput input, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null) return ServiceResult.Failure("Catégorie introuvable.");
        var name = input.Name.Trim();
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(input.Slug) ? name : input.Slug);
        var error = await ValidateUniqueAsync(name, slug, id, cancellationToken);
        if (error is not null) return ServiceResult.Failure(error);
        category.Name = name;
        category.Slug = slug;
        category.Description = NullIfWhiteSpace(input.Description);
        category.ImageUrl = NullIfWhiteSpace(input.ImageUrl);
        category.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null) return ServiceResult.Failure("Catégorie introuvable.");
        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.Include(category => category.Trainings)
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null) return ServiceResult.Failure("Catégorie introuvable.");
        if (category.Trainings.Count != 0)
            return ServiceResult.Failure("Cette catégorie contient des formations et ne peut pas être supprimée.");
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<string?> ValidateUniqueAsync(string name, string slug, int? excludedId, CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLower();
        if (await dbContext.Categories.AnyAsync(category =>
                category.Id != excludedId && category.Name.ToLower() == normalizedName, cancellationToken))
            return "Une catégorie portant ce nom existe déjà.";
        if (await dbContext.Categories.AnyAsync(category =>
                category.Id != excludedId && category.Slug == slug, cancellationToken))
            return "Ce slug est déjà utilisé.";
        return null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
