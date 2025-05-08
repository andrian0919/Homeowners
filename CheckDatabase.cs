using System;
using System.Linq;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Utils
{
    public static class CheckDatabase
    {
        public static async Task CheckReportsAndParameters(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("CheckDatabase");
            
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                logger.LogInformation("Checking report definitions and parameters in database...");
                
                // Check if report definitions exist
                var reportDefinitions = await context.ReportDefinitions
                    .AsNoTracking()
                    .ToListAsync();
                
                if (reportDefinitions.Any())
                {
                    logger.LogInformation($"Found {reportDefinitions.Count} report definitions:");
                    foreach (var report in reportDefinitions)
                    {
                        logger.LogInformation($"  - [{report.Id}] {report.Name} ({report.Category})");
                        
                        // Check parameters for this report
                        var parameters = await context.ReportParameters
                            .Where(p => p.ReportDefinitionId == report.Id)
                            .AsNoTracking()
                            .ToListAsync();
                        
                        if (parameters.Any())
                        {
                            logger.LogInformation($"    Parameters: {parameters.Count}");
                            foreach (var param in parameters)
                            {
                                logger.LogInformation($"      - [{param.Id}] {param.Name} ({param.Type})");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"    No parameters found for report {report.Name}");
                        }
                    }
                }
                else
                {
                    logger.LogWarning("No report definitions found in database.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking database");
            }
        }
    }
} 