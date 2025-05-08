using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HomeownersSubdivision.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;

namespace HomeownersSubdivision.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Look for any existing data
                if (!context.Users.Any())
                {
                    // Add seed initialization logic here if needed
                }
                
                // Seed forum categories if they don't exist
                if (!context.ForumCategories.Any())
                {
                    context.ForumCategories.AddRange(
                        new Models.ForumCategory
                        {
                            Name = "General Discussion",
                            Description = "Discuss any general topics related to our community",
                            DisplayOrder = 1,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Models.ForumCategory
                        {
                            Name = "Announcements",
                            Description = "Important community announcements and updates",
                            DisplayOrder = 2,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Models.ForumCategory
                        {
                            Name = "Events & Activities",
                            Description = "Discuss upcoming events and activities in our neighborhood",
                            DisplayOrder = 3,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Models.ForumCategory
                        {
                            Name = "Facilities",
                            Description = "Discussions about our community facilities and amenities",
                            DisplayOrder = 4,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Models.ForumCategory
                        {
                            Name = "Help & Support",
                            Description = "Ask questions and get help from community members",
                            DisplayOrder = 5,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        }
                    );
                    
                    context.SaveChanges();
                }
            }
        }

        private static async Task SeedReportsAsync(ApplicationDbContext context)
        {
            // Check if there are any report definitions
            if (!await context.ReportDefinitions.AnyAsync())
            {
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
                
                context.ReportDefinitions.AddRange(financialReport, serviceRequestReport, communityEngagementReport);
                await context.SaveChangesAsync();
                
                // Create report parameters for the financial report
                var startDateParam = new ReportParameter
                {
                    ReportDefinitionId = financialReport.Id,
                    Name = "startDate",
                    Label = "Start Date",
                    Type = ParameterType.DateTime,
                    DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                    IsRequired = true,
                    SortOrder = 1
                };
                
                var endDateParam = new ReportParameter
                {
                    ReportDefinitionId = financialReport.Id,
                    Name = "endDate",
                    Label = "End Date",
                    Type = ParameterType.DateTime,
                    DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                    IsRequired = true,
                    SortOrder = 2
                };
                
                context.ReportParameters.AddRange(startDateParam, endDateParam);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager)
        {
            // ... existing seeding code
            
            // Seed reports
            await SeedReportsAsync(context);
        }
    }
} 