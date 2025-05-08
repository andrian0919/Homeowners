using System;
using System.Linq;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Services
{
    public class DatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(ApplicationDbContext context, ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();
                
                // Seed forum categories if none exist
                if (!await _context.ForumCategories.AnyAsync())
                {
                    _logger.LogInformation("Seeding forum categories...");
                    
                    var categories = new[]
                    {
                        new ForumCategory 
                        { 
                            Name = "General Discussion", 
                            Description = "Discuss any general topics related to our community", 
                            DisplayOrder = 1,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Announcements", 
                            Description = "Important community announcements and updates", 
                            DisplayOrder = 2,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Events & Activities", 
                            Description = "Discuss upcoming events and activities in our neighborhood", 
                            DisplayOrder = 3,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Facilities", 
                            Description = "Discussions about our community facilities and amenities", 
                            DisplayOrder = 4,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Help & Support", 
                            Description = "Ask questions and get help from community members", 
                            DisplayOrder = 5,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        }
                    };
                    
                    await _context.ForumCategories.AddRangeAsync(categories);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Forum categories seeded successfully.");
                }
                
                // Seed report definitions if none exist
                if (!await _context.ReportDefinitions.AnyAsync())
                {
                    _logger.LogInformation("Seeding report definitions...");
                    
                    // Create default report definitions
                    var financialReport = new ReportDefinition
                    {
                        Name = "Financial Summary Report",
                        Description = "Provides a summary of bills issued, payments received, and financial trends",
                        Category = ReportCategory.Finance,
                        QueryDefinition = "FinancialSummary",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    };
                    
                    var serviceRequestReport = new ReportDefinition
                    {
                        Name = "Service Requests Report",
                        Description = "Analyzes service requests by status, type, priority, and resolution times",
                        Category = ReportCategory.ServiceRequests,
                        QueryDefinition = "ServiceRequestsSummary",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    };
                    
                    var communityEngagementReport = new ReportDefinition
                    {
                        Name = "Community Engagement Report",
                        Description = "Provides metrics on forum activity, event participation, and community interaction",
                        Category = ReportCategory.CommunityEngagement,
                        QueryDefinition = "CommunityEngagementSummary",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now
                    };
                    
                    await _context.ReportDefinitions.AddRangeAsync(financialReport, serviceRequestReport, communityEngagementReport);
                    await _context.SaveChangesAsync();
                    
                    // Create report parameters for each report
                    var parameters = new[]
                    {
                        // Financial Report Parameters
                        new ReportParameter
                        {
                            ReportDefinitionId = financialReport.Id,
                            Name = "startDate",
                            Label = "Start Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 1
                        },
                        new ReportParameter
                        {
                            ReportDefinitionId = financialReport.Id,
                            Name = "endDate",
                            Label = "End Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 2
                        },
                        
                        // Service Requests Report Parameters
                        new ReportParameter
                        {
                            ReportDefinitionId = serviceRequestReport.Id,
                            Name = "startDate",
                            Label = "Start Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 1
                        },
                        new ReportParameter
                        {
                            ReportDefinitionId = serviceRequestReport.Id,
                            Name = "endDate",
                            Label = "End Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 2
                        },
                        new ReportParameter
                        {
                            ReportDefinitionId = serviceRequestReport.Id,
                            Name = "status",
                            Label = "Status Filter",
                            Type = ParameterType.Enum,
                            Options = "New,InProgress,Completed,Cancelled",
                            IsRequired = false,
                            SortOrder = 3
                        },
                        
                        // Community Engagement Report Parameters
                        new ReportParameter
                        {
                            ReportDefinitionId = communityEngagementReport.Id,
                            Name = "startDate",
                            Label = "Start Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 1
                        },
                        new ReportParameter
                        {
                            ReportDefinitionId = communityEngagementReport.Id,
                            Name = "endDate",
                            Label = "End Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 2
                        }
                    };
                    
                    await _context.ReportParameters.AddRangeAsync(parameters);
                    await _context.SaveChangesAsync();
                    
                    // Seed some dashboard widgets
                    var financialWidget = new DashboardWidget
                    {
                        Title = "Financial Summary",
                        Description = "Overview of financial statistics",
                        Type = WidgetType.Counter,
                        Size = WidgetSize.Medium,
                        SortOrder = 1,
                        ReportDefinitionId = financialReport.Id,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        VisibleToRoles = 1 // Admin only
                    };
                    
                    var serviceRequestWidget = new DashboardWidget
                    {
                        Title = "Service Requests",
                        Description = "Service request statistics",
                        Type = WidgetType.BarChart,
                        Size = WidgetSize.Medium,
                        SortOrder = 2,
                        ReportDefinitionId = serviceRequestReport.Id,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        VisibleToRoles = 3 // Admin and Staff
                    };
                    
                    var communityWidget = new DashboardWidget
                    {
                        Title = "Community Engagement",
                        Description = "Forum and community activity",
                        Type = WidgetType.LineChart,
                        Size = WidgetSize.Large,
                        SortOrder = 3,
                        ReportDefinitionId = communityEngagementReport.Id,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        VisibleToRoles = 1 // Admin only
                    };
                    
                    await _context.DashboardWidgets.AddRangeAsync(financialWidget, serviceRequestWidget, communityWidget);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Report definitions and dashboard widgets seeded successfully.");
                }
                // Check if parameters exist for all reports
                else if (!await _context.ReportParameters.AnyAsync())
                {
                    _logger.LogInformation("Report definitions exist but parameters are missing. Adding parameters...");
                    
                    var reports = await _context.ReportDefinitions.ToListAsync();
                    
                    foreach (var report in reports)
                    {
                        var parameters = new List<ReportParameter>();
                        
                        // Add common date parameters to all reports
                        parameters.Add(new ReportParameter
                        {
                            ReportDefinitionId = report.Id,
                            Name = "startDate",
                            Label = "Start Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 1
                        });
                        
                        parameters.Add(new ReportParameter
                        {
                            ReportDefinitionId = report.Id,
                            Name = "endDate",
                            Label = "End Date",
                            Type = ParameterType.DateTime,
                            DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                            IsRequired = true,
                            SortOrder = 2
                        });
                        
                        // Add specific parameters based on report category
                        if (report.Category == ReportCategory.ServiceRequests)
                        {
                            parameters.Add(new ReportParameter
                            {
                                ReportDefinitionId = report.Id,
                                Name = "status",
                                Label = "Status Filter",
                                Type = ParameterType.Enum,
                                Options = "New,InProgress,Completed,Cancelled",
                                IsRequired = false,
                                SortOrder = 3
                            });
                        }
                        
                        await _context.ReportParameters.AddRangeAsync(parameters);
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Report parameters added successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
    }
} 