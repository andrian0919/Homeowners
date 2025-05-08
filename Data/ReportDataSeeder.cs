using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;

namespace HomeownersSubdivision.Data
{
    public static class ReportDataSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if the database already has report definitions
                if (context.ReportDefinitions.Any())
                {
                    return; // Report definitions have already been seeded
                }

                // Get admin user for creator attribution
                var adminUser = context.Users.FirstOrDefault(u => u.Role == UserRole.Administrator);
                int? adminId = adminUser?.Id;

                // Create report definitions
                var reportDefinitions = new List<ReportDefinition>
                {
                    new ReportDefinition
                    {
                        Name = "Financial Summary Report",
                        Description = "Provides a summary of bills issued, payments received, and financial trends",
                        Category = ReportCategory.Finance,
                        QueryDefinition = "BillsAndPayments",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        CreatedById = adminId
                    },
                    new ReportDefinition
                    {
                        Name = "Service Requests Report",
                        Description = "Analyzes service requests by status, type, priority, and resolution times",
                        Category = ReportCategory.ServiceRequests,
                        QueryDefinition = "ServiceRequests",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        CreatedById = adminId
                    },
                    new ReportDefinition
                    {
                        Name = "Community Engagement Report",
                        Description = "Provides metrics on forum activity, event participation, and community interaction",
                        Category = ReportCategory.CommunityEngagement,
                        QueryDefinition = "CommunityEngagement",
                        IsBuiltIn = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.Now,
                        CreatedById = adminId
                    }
                };

                context.ReportDefinitions.AddRange(reportDefinitions);
                context.SaveChanges();

                // Add parameters for each report
                var reportParameters = new List<ReportParameter>();

                // Parameters for Financial Summary Report
                reportParameters.AddRange(new List<ReportParameter>
                {
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[0].Id,
                        Name = "startDate",
                        Label = "Start Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 1
                    },
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[0].Id,
                        Name = "endDate",
                        Label = "End Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 2
                    }
                });

                // Parameters for Service Requests Report
                reportParameters.AddRange(new List<ReportParameter>
                {
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[1].Id,
                        Name = "startDate",
                        Label = "Start Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 1
                    },
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[1].Id,
                        Name = "endDate",
                        Label = "End Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 2
                    },
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[1].Id,
                        Name = "status",
                        Label = "Status Filter",
                        Type = ParameterType.Enum,
                        Options = "New,InProgress,Completed,Cancelled",
                        IsRequired = false,
                        SortOrder = 3
                    }
                });

                // Parameters for Community Engagement Report
                reportParameters.AddRange(new List<ReportParameter>
                {
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[2].Id,
                        Name = "startDate",
                        Label = "Start Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 1
                    },
                    new ReportParameter
                    {
                        ReportDefinitionId = reportDefinitions[2].Id,
                        Name = "endDate",
                        Label = "End Date",
                        Type = ParameterType.DateTime,
                        DefaultValue = DateTime.Today.ToString("yyyy-MM-dd"),
                        IsRequired = true,
                        SortOrder = 2
                    }
                });

                context.ReportParameters.AddRange(reportParameters);
                context.SaveChanges();
            }
        }
    }
} 