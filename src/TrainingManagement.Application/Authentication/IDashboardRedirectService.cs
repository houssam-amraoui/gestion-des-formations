namespace TrainingManagement.Application.Authentication;

public interface IDashboardRedirectService
{
    DashboardDestination GetDestination(IEnumerable<string> roles);
}
