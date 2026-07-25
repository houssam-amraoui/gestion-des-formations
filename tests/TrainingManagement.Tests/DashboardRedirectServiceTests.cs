using TrainingManagement.Application.Authentication;

namespace TrainingManagement.Tests;

public sealed class DashboardRedirectServiceTests
{
    private readonly DashboardRedirectService _service = new();

    [Theory]
    [InlineData("Admin", "Admin")]
    [InlineData("Trainer", "Trainer")]
    [InlineData("Learner", "Learner")]
    public void GetDestination_RedirectsToExpectedArea(string role, string expectedArea)
    {
        var result = _service.GetDestination([role]);
        Assert.Equal(expectedArea, result.Area);
        Assert.Equal("Dashboard", result.Controller);
        Assert.Equal("Index", result.Action);
    }
}
