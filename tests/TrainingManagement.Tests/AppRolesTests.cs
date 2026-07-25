using TrainingManagement.Domain.Constants;

namespace TrainingManagement.Tests;

public sealed class AppRolesTests
{
    [Fact]
    public void Roles_AreCentralizedAndDistinct()
    {
        Assert.Equal("Admin", AppRoles.Admin);
        Assert.Equal("Trainer", AppRoles.Trainer);
        Assert.Equal("Learner", AppRoles.Learner);
        Assert.Equal(3, AppRoles.All.Distinct().Count());
    }
}
