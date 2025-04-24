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

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Use Startup.cs to configure the request pipeline
startup.Configure(app, app.Environment);

app.Run(); 