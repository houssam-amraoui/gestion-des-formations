using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrainingManagement.Application.AiTrainer;
using TrainingManagement.Application.Common;
using TrainingManagement.Domain.Entities;
using TrainingManagement.Infrastructure.AiTrainer;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Infrastructure.Services;

public sealed class AiConsentService(ApplicationDbContext db, IOptions<AiTrainerOptions> options) :
    IAiConsentService
{
    private const string ConsentType = "AudioProcessing";

    public async Task<AiConsentModel> GetAsync(string userId, string provider,
        CancellationToken token = default)
    {
        var version = options.Value.ConsentPolicyVersion;
        var consent = await db.AiUserConsents.AsNoTracking()
            .Where(x => x.UserId == userId && x.ConsentType == ConsentType &&
                x.Provider == provider && x.PolicyVersion == version)
            .OrderByDescending(x => x.AcceptedAt).FirstOrDefaultAsync(token);
        return new(consent?.IsValid(provider, version) == true, provider, version, consent?.AcceptedAt);
    }

    public async Task<ServiceResult> AcceptAsync(string userId, string provider,
        CancellationToken token = default)
    {
        var current = await db.AiUserConsents.Where(x => x.UserId == userId &&
            x.ConsentType == ConsentType && x.Provider == provider &&
            x.PolicyVersion == options.Value.ConsentPolicyVersion && x.RevokedAt == null)
            .FirstOrDefaultAsync(token);
        if (current is not null) return ServiceResult.Success();
        db.AiUserConsents.Add(new AiUserConsent
        {
            UserId = userId, Provider = provider, ConsentType = ConsentType,
            PolicyVersion = options.Value.ConsentPolicyVersion,
            AcceptedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RevokeAsync(string userId, string provider,
        CancellationToken token = default)
    {
        var consents = await db.AiUserConsents.Where(x => x.UserId == userId &&
            x.ConsentType == ConsentType && x.Provider == provider && x.RevokedAt == null).ToListAsync(token);
        foreach (var consent in consents) consent.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return ServiceResult.Success();
    }
}
