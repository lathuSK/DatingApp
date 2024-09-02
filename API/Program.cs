using API;
using API.Data;
using API.Entities;
using API.Extensions;
using API.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

// Configure CORS
app.UseCors(policy =>
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()
          .WithOrigins(
              "http://localhost:4200",
              "https://localhost:4200",
              "https://dating-app-by-lathusk-gxczdqgxftbbbxfu.southindia-01.azurewebsites.net/"
          )
);

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();  // Serve default files (e.g., index.html)
app.UseStaticFiles();   // Serve static files (e.g., JS, CSS)

// Configure routing
app.MapControllers();
app.MapHub<PresenceHub>("hubs/presence");
app.MapHub<MessageHub>("hubs/message");

// Fallback to Angular app for client-side routing
app.MapFallbackToFile("index.html");  // Use MapFallbackToFile for static files

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<DataContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    await context.Database.MigrateAsync();
    await context.Database.ExecuteSqlRawAsync("DELETE FROM [Connections]");
    await Seed.SeedUsers(userManager, roleManager);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration");
}

app.Run();
