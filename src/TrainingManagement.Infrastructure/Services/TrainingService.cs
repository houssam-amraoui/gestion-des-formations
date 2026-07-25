using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Common;
using TrainingManagement.Application.Trainings;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Domain.Enums;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class TrainingService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ITrainingService
{
    public async Task<PagedResult<TrainingSummary>> GetAdminPagedAsync(TrainingQuery query, CancellationToken cancellationToken = default)
    {
        var source = BuildSummaryQuery();
        if (!string.IsNullOrWhiteSpace(query.Search)) source = source.Where(item => item.Title.Contains(query.Search.Trim()));
        if (query.CategoryId.HasValue) source = source.Where(item => item.CategoryId == query.CategoryId);
        if (query.Level.HasValue) source = source.Where(item => item.Level == query.Level);
        if (query.Status.HasValue) source = source.Where(item => item.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.TrainerId)) source = source.Where(item => item.TrainerId == query.TrainerId);
        source = query.Sort switch
        {
            "created_asc" => source.OrderBy(item => item.CreatedAt),
            "title_asc" => source.OrderBy(item => item.Title),
            "title_desc" => source.OrderByDescending(item => item.Title),
            _ => source.OrderByDescending(item => item.CreatedAt)
        };
        return await PageAsync(source, query.Page, query.PageSize, cancellationToken);
    }

    public async Task<PagedResult<TrainingSummary>> GetPublishedPagedAsync(PublicTrainingQuery query, CancellationToken cancellationToken = default)
    {
        var source = BuildSummaryQuery().Where(item => item.Status == TrainingStatus.Published);
        if (!string.IsNullOrWhiteSpace(query.Search)) source = source.Where(item => item.Title.Contains(query.Search.Trim()));
        if (query.CategoryId.HasValue) source = source.Where(item => item.CategoryId == query.CategoryId);
        if (query.Level.HasValue) source = source.Where(item => item.Level == query.Level);
        return await PageAsync(source.OrderByDescending(item => item.PublishedAt), query.Page, query.PageSize, cancellationToken);
    }

    public Task<TrainingDetails?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        BuildDetailsQuery(dbContext.Trainings.Where(item => item.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<TrainingDetails?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        BuildDetailsQuery(dbContext.Trainings.Where(
                item => item.Slug == slug && item.Status == TrainingStatus.Published))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<TrainerOption>> GetTrainerOptionsAsync(CancellationToken cancellationToken = default)
    {
        var trainers = await userManager.GetUsersInRoleAsync(AppRoles.Trainer);
        return trainers.Where(user => user.IsActive)
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Select(user => new TrainerOption(user.Id, $"{user.FullName} — {user.Email}"))
            .ToArray();
    }

    public async Task<ServiceResult<int>> CreateAsync(TrainingInput input, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateInputAsync(input, null, cancellationToken);
        if (validation.Error is not null) return ServiceResult<int>.Failure(validation.Error);
        var training = new Training
        {
            Title = input.Title.Trim(),
            Slug = validation.Slug,
            ShortDescription = input.ShortDescription.Trim(),
            Description = input.Description.Trim(),
            ThumbnailUrl = NullIfWhiteSpace(input.ThumbnailUrl),
            CategoryId = input.CategoryId,
            TrainerId = NullIfWhiteSpace(input.TrainerId),
            Level = input.Level,
            Language = input.Language.Trim(),
            EstimatedDurationHours = input.EstimatedDurationHours,
            Price = input.Price,
            IsFree = input.IsFree,
            Status = TrainingStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        training.ApplyPricing();
        dbContext.Trainings.Add(training);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(training.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int id, TrainingInput input, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.FindAsync([id], cancellationToken);
        if (training is null) return ServiceResult.Failure("Formation introuvable.");
        if (training.Status == TrainingStatus.Archived)
            return ServiceResult.Failure("Une formation archivée ne peut plus être modifiée.");
        var validation = await ValidateInputAsync(input, id, cancellationToken);
        if (validation.Error is not null) return ServiceResult.Failure(validation.Error);
        training.Title = input.Title.Trim();
        training.Slug = validation.Slug;
        training.ShortDescription = input.ShortDescription.Trim();
        training.Description = input.Description.Trim();
        training.ThumbnailUrl = NullIfWhiteSpace(input.ThumbnailUrl);
        training.CategoryId = input.CategoryId;
        training.TrainerId = NullIfWhiteSpace(input.TrainerId);
        training.Level = input.Level;
        training.Language = input.Language.Trim();
        training.EstimatedDurationHours = input.EstimatedDurationHours;
        training.Price = input.Price;
        training.IsFree = input.IsFree;
        training.ApplyPricing();
        training.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (training is null) return ServiceResult.Failure("Formation introuvable.");
        if (training.Status == TrainingStatus.Archived) return ServiceResult.Failure("Une formation archivée ne peut pas être publiée.");
        try { training.Publish(DateTime.UtcNow); }
        catch (InvalidOperationException exception) { return ServiceResult.Failure(exception.Message); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UnpublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.FindAsync([id], cancellationToken);
        if (training is null) return ServiceResult.Failure("Formation introuvable.");
        try { training.Unpublish(DateTime.UtcNow); }
        catch (InvalidOperationException exception) { return ServiceResult.Failure(exception.Message); }
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var training = await dbContext.Trainings.FindAsync([id], cancellationToken);
        if (training is null) return ServiceResult.Failure("Formation introuvable.");
        training.Archive(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<(string Slug, string? Error)> ValidateInputAsync(TrainingInput input, int? excludedId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.CategoryId, cancellationToken);
        if (category is null || !category.IsActive) return ("", "La formation doit appartenir à une catégorie active.");
        if (input.EstimatedDurationHours <= 0) return ("", "La durée doit être supérieure à zéro.");
        if (input.Price < 0) return ("", "Le prix ne peut pas être négatif.");
        if (!string.IsNullOrWhiteSpace(input.TrainerId))
        {
            var trainer = await userManager.FindByIdAsync(input.TrainerId);
            if (trainer is null || !trainer.IsActive || !await userManager.IsInRoleAsync(trainer, AppRoles.Trainer))
                return ("", "Le formateur sélectionné est invalide.");
        }
        var slug = SlugGenerator.Generate(string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
        if (string.IsNullOrWhiteSpace(slug)) return ("", "Le slug est invalide.");
        if (await dbContext.Trainings.AnyAsync(item => item.Id != excludedId && item.Slug == slug, cancellationToken))
            return ("", "Ce slug est déjà utilisé.");
        return (slug, null);
    }

    private IQueryable<TrainingProjection> BuildSummaryQuery() =>
        from training in dbContext.Trainings.AsNoTracking()
        join user in dbContext.Users.AsNoTracking() on training.TrainerId equals user.Id into users
        from trainer in users.DefaultIfEmpty()
        select new TrainingProjection
        {
            Id = training.Id, Title = training.Title, Slug = training.Slug,
            ShortDescription = training.ShortDescription, ThumbnailUrl = training.ThumbnailUrl,
            CategoryId = training.CategoryId, CategoryName = training.Category.Name,
            TrainerId = training.TrainerId,
            TrainerName = trainer == null ? null : trainer.FirstName + " " + trainer.LastName,
            Level = training.Level, Duration = training.EstimatedDurationHours,
            Price = training.Price, IsFree = training.IsFree, Status = training.Status,
            CreatedAt = training.CreatedAt, PublishedAt = training.PublishedAt
        };

    private IQueryable<TrainingDetails> BuildDetailsQuery(IQueryable<Training> trainings) =>
        from training in trainings.AsNoTracking()
        join user in dbContext.Users.AsNoTracking() on training.TrainerId equals user.Id into users
        from trainer in users.DefaultIfEmpty()
        select new TrainingDetails(training.Id, training.Title, training.Slug, training.ShortDescription,
            training.Description, training.ThumbnailUrl, training.CategoryId, training.Category.Name,
            training.TrainerId, trainer == null ? null : trainer.FirstName + " " + trainer.LastName,
            training.Level, training.Language, training.EstimatedDurationHours, training.Price,
            training.IsFree, training.Status, training.PublishedAt, training.CreatedAt, training.UpdatedAt);

    private static async Task<PagedResult<TrainingSummary>> PageAsync(
        IQueryable<TrainingProjection> query, int requestedPage, int requestedPageSize, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, requestedPage);
        var pageSize = Math.Clamp(requestedPageSize, 1, 50);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new TrainingSummary(item.Id, item.Title, item.Slug, item.ShortDescription, item.ThumbnailUrl,
                item.CategoryName, item.TrainerName, item.Level, item.Duration, item.Price,
                item.IsFree, item.Status, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, total);
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class TrainingProjection
    {
        public int Id { get; init; }
        public required string Title { get; init; }
        public required string Slug { get; init; }
        public required string ShortDescription { get; init; }
        public string? ThumbnailUrl { get; init; }
        public int CategoryId { get; init; }
        public required string CategoryName { get; init; }
        public string? TrainerId { get; init; }
        public string? TrainerName { get; init; }
        public TrainingLevel Level { get; init; }
        public int Duration { get; init; }
        public decimal Price { get; init; }
        public bool IsFree { get; init; }
        public TrainingStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? PublishedAt { get; init; }
    }
}
