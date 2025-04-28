using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Services;
using HomeownersSubdivision.Data;
using HomeownersSubdivision;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Use Startup.cs for configuration
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Ensure database creation and initialize with seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Create the database if it doesn't exist
        context.Database.EnsureCreated();
        
        // Log database creation status
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database checked and created if needed.");
        
        // Initialize with seed data
        SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating or seeding the database.");
    }
}

// Use Startup.cs to configure the request pipeline
startup.Configure(app, app.Environment);

app.Run(); 