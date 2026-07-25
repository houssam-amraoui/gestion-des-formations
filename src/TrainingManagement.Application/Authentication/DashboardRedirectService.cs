using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Application.Authentication;

public sealed class DashboardRedirectService : IDashboardRedirectService
{
    public DashboardDestination GetDestination(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleSet.Contains(AppRoles.Admin))
            return new("Admin", "Dashboard", "Index");
        if (roleSet.Contains(AppRoles.Trainer))
            return new("Trainer", "Dashboard", "Index");

        return new("Learner", "Dashboard", "Index");
    }
}
