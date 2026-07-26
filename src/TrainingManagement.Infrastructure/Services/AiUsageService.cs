using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AiUsageService(ApplicationDbContext db) : IAiUsageService
{
    public Task<AiUsageSummary> GetAdminAsync(DateTime from, DateTime to,
        CancellationToken token = default) => Calculate(db.AiConversationSessions.AsNoTracking()
            .Where(x => x.StartedAt >= from && x.StartedAt < to), token);

    public Task<AiUsageSummary> GetTrainerAsync(string trainerId, DateTime from, DateTime to,
        CancellationToken token = default) => Calculate(db.AiConversationSessions.AsNoTracking()
            .Where(x => x.AiTrainerProfile.Training.TrainerId == trainerId &&
                x.StartedAt >= from && x.StartedAt < to), token);

    public Task<AiUsageSummary> GetLearnerAsync(string userId, DateTime from, DateTime to,
        CancellationToken token = default) => Calculate(db.AiConversationSessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.StartedAt >= from && x.StartedAt < to), token);

    private static async Task<AiUsageSummary> Calculate(IQueryable<Domain.Entities.AiConversationSession> query,
        CancellationToken token)
    {
        var rows = await query.Select(x => new
        {
            x.Id, x.UserId, Provider = x.AiTrainerProfile.Provider,
            Training = x.AiTrainerProfile.Training.Title, x.MessageCount,
            Audio = x.InputAudioSeconds + x.OutputAudioSeconds, x.EstimatedCost,
            Failed = x.Status == Domain.Enums.AiConversationStatus.Failed ||
                x.UsageRecords.Any(u => !u.Succeeded)
        }).ToListAsync(token);
        var sessions = rows.Count;
        return new(sessions, rows.Select(x => x.UserId).Distinct().Count(),
            rows.Sum(x => x.MessageCount), rows.Sum(x => x.Audio),
            rows.Count(x => x.Failed), sessions == 0 ? 0 :
                Math.Round(rows.Count(x => x.Failed) * 100m / sessions, 2),
            rows.Any(x => x.EstimatedCost is not null) ? rows.Sum(x => x.EstimatedCost) : null,
            rows.GroupBy(x => x.Provider).Select(g => new AiUsageBreakdown(g.Key, g.Count(),
                g.Sum(x => x.MessageCount), g.Any(x => x.EstimatedCost is not null)
                    ? g.Sum(x => x.EstimatedCost) : null)).OrderByDescending(x => x.Sessions).ToArray(),
            rows.GroupBy(x => x.Training).Select(g => new AiUsageBreakdown(g.Key, g.Count(),
                g.Sum(x => x.MessageCount), g.Any(x => x.EstimatedCost is not null)
                    ? g.Sum(x => x.EstimatedCost) : null)).OrderByDescending(x => x.Sessions).ToArray());
    }
}
