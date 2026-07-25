using Microsoft.EntityFrameworkCore;
using TrainingManagement.Application.Authentication;
using TrainingManagement.Infrastructure.Configuration;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IDashboardRedirectService, DashboardRedirectService>();
builder.Services.AddTrainingManagementInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

await using (var seedScope = app.Services.CreateAsyncScope())
{
    var seeder = seedScope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;
