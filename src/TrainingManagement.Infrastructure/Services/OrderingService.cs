using Microsoft.EntityFrameworkCore;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

internal static class OrderingService
{
    public static async Task<int> PrepareModuleInsertAsync(ApplicationDbContext db, int trainingId, int? requested, CancellationToken token)
    {
        var items = await db.TrainingModules.Where(x => x.TrainingId == trainingId)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order).ToListAsync(token);
        var order = Math.Clamp(requested ?? items.Count + 1, 1, items.Count + 1);
        await ShiftAsync(db, items.Where(x => x.Order >= order), x => x.Order, (x, value) => x.Order = value, token);
        return order;
    }

    public static async Task<int> PrepareLessonInsertAsync(ApplicationDbContext db, int moduleId, int? requested, CancellationToken token)
    {
        var items = await db.Lessons.Where(x => x.TrainingModuleId == moduleId)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order).ToListAsync(token);
        var order = Math.Clamp(requested ?? items.Count + 1, 1, items.Count + 1);
        await ShiftAsync(db, items.Where(x => x.Order >= order), x => x.Order, (x, value) => x.Order = value, token);
        return order;
    }

    public static async Task<int> PrepareContentInsertAsync(ApplicationDbContext db, int lessonId, int? requested, CancellationToken token)
    {
        var items = await db.LessonContents.Where(x => x.LessonId == lessonId).OrderBy(x => x.Order).ToListAsync(token);
        var order = Math.Clamp(requested ?? items.Count + 1, 1, items.Count + 1);
        await ShiftAsync(db, items.Where(x => x.Order >= order), x => x.Order, (x, value) => x.Order = value, token);
        return order;
    }

    public static Task NormalizeModulesAsync(ApplicationDbContext db, int parentId, CancellationToken token) =>
        NormalizeAsync(db, db.TrainingModules.Where(x => x.TrainingId == parentId)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order), (x, value) => x.Order = value, token);

    public static Task NormalizeLessonsAsync(ApplicationDbContext db, int parentId, CancellationToken token) =>
        NormalizeAsync(db, db.Lessons.Where(x => x.TrainingModuleId == parentId)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order), (x, value) => x.Order = value, token);

    public static Task NormalizeContentsAsync(ApplicationDbContext db, int parentId, CancellationToken token) =>
        NormalizeAsync(db, db.LessonContents.Where(x => x.LessonId == parentId)
            .OrderBy(x => x.Order), (x, value) => x.Order = value, token);

    public static async Task RepositionModuleAsync(ApplicationDbContext db, TrainingModule current, int requested, CancellationToken token)
    {
        var items = await db.TrainingModules.Where(x => x.TrainingId == current.TrainingId && x.Id != current.Id)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order).ToListAsync(token);
        items.Insert(Math.Clamp(requested, 1, items.Count + 1) - 1, current);
        await AssignAsync(db, items, (x, value) => x.Order = value, token);
    }

    public static async Task RepositionLessonAsync(ApplicationDbContext db, Lesson current, int requested, CancellationToken token)
    {
        var items = await db.Lessons.Where(x => x.TrainingModuleId == current.TrainingModuleId && x.Id != current.Id)
            .OrderBy(x => x.IsArchived).ThenBy(x => x.Order).ToListAsync(token);
        items.Insert(Math.Clamp(requested, 1, items.Count + 1) - 1, current);
        await AssignAsync(db, items, (x, value) => x.Order = value, token);
    }

    public static async Task RepositionContentAsync(ApplicationDbContext db, LessonContent current, int requested, CancellationToken token)
    {
        var items = await db.LessonContents.Where(x => x.LessonId == current.LessonId && x.Id != current.Id)
            .OrderBy(x => x.Order).ToListAsync(token);
        items.Insert(Math.Clamp(requested, 1, items.Count + 1) - 1, current);
        await AssignAsync(db, items, (x, value) => x.Order = value, token);
    }

    public static async Task<bool> MoveModuleAsync(ApplicationDbContext db, int id, int direction, CancellationToken token)
    {
        var current = await db.TrainingModules.FindAsync([id], token);
        if (current is null || current.IsArchived) return false;
        var sibling = await db.TrainingModules.Where(x => x.TrainingId == current.TrainingId && !x.IsArchived)
            .Where(x => direction < 0 ? x.Order < current.Order : x.Order > current.Order)
            .OrderBy(x => direction < 0 ? -x.Order : x.Order).FirstOrDefaultAsync(token);
        return await SwapAsync(db, current, sibling, x => x.Order, (x, value) => x.Order = value, token);
    }

    public static async Task<bool> MoveLessonAsync(ApplicationDbContext db, int id, int direction, CancellationToken token)
    {
        var current = await db.Lessons.FindAsync([id], token);
        if (current is null || current.IsArchived) return false;
        var sibling = await db.Lessons.Where(x => x.TrainingModuleId == current.TrainingModuleId && !x.IsArchived)
            .Where(x => direction < 0 ? x.Order < current.Order : x.Order > current.Order)
            .OrderBy(x => direction < 0 ? -x.Order : x.Order).FirstOrDefaultAsync(token);
        return await SwapAsync(db, current, sibling, x => x.Order, (x, value) => x.Order = value, token);
    }

    public static async Task<bool> MoveContentAsync(ApplicationDbContext db, int id, int direction, CancellationToken token)
    {
        var current = await db.LessonContents.FindAsync([id], token);
        if (current is null) return false;
        var sibling = await db.LessonContents.Where(x => x.LessonId == current.LessonId)
            .Where(x => direction < 0 ? x.Order < current.Order : x.Order > current.Order)
            .OrderBy(x => direction < 0 ? -x.Order : x.Order).FirstOrDefaultAsync(token);
        return await SwapAsync(db, current, sibling, x => x.Order, (x, value) => x.Order = value, token);
    }

    private static async Task ShiftAsync<T>(ApplicationDbContext db, IEnumerable<T> source,
        Func<T, int> getOrder, Action<T, int> setOrder, CancellationToken token) where T : class
    {
        var items = source.ToArray();
        foreach (var item in items) setOrder(item, getOrder(item) + 10000);
        await db.SaveChangesAsync(token);
        foreach (var item in items) setOrder(item, getOrder(item) - 9999);
        await db.SaveChangesAsync(token);
    }

    private static async Task NormalizeAsync<T>(ApplicationDbContext db, IQueryable<T> query,
        Action<T, int> setOrder, CancellationToken token) where T : class
    {
        var items = await query.ToListAsync(token);
        await AssignAsync(db, items, setOrder, token);
    }

    private static async Task AssignAsync<T>(ApplicationDbContext db, IReadOnlyList<T> items,
        Action<T, int> setOrder, CancellationToken token) where T : class
    {
        for (var index = 0; index < items.Count; index++) setOrder(items[index], 10001 + index);
        await db.SaveChangesAsync(token);
        for (var index = 0; index < items.Count; index++) setOrder(items[index], index + 1);
        await db.SaveChangesAsync(token);
    }

    private static async Task<bool> SwapAsync<T>(ApplicationDbContext db, T current, T? sibling,
        Func<T, int> getOrder, Action<T, int> setOrder, CancellationToken token) where T : class
    {
        if (sibling is null) return true;
        var currentOrder = getOrder(current);
        var siblingOrder = getOrder(sibling);
        setOrder(current, -1);
        await db.SaveChangesAsync(token);
        setOrder(sibling, currentOrder);
        await db.SaveChangesAsync(token);
        setOrder(current, siblingOrder);
        await db.SaveChangesAsync(token);
        return true;
    }
}
