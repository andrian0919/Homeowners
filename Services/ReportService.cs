using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Report Definition Management

        public async Task<List<ReportDefinition>> GetAllReportDefinitionsAsync()
        {
            return await _context.ReportDefinitions
                .OrderBy(r => r.Category)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<ReportDefinition?> GetReportDefinitionByIdAsync(int id)
        {
            return await _context.ReportDefinitions.FindAsync(id);
        }

        public async Task<ReportDefinition> CreateReportDefinitionAsync(ReportDefinition reportDefinition)
        {
            _context.ReportDefinitions.Add(reportDefinition);
            await _context.SaveChangesAsync();
            return reportDefinition;
        }

        public async Task<ReportDefinition?> UpdateReportDefinitionAsync(ReportDefinition reportDefinition)
        {
            var existingReport = await _context.ReportDefinitions.FindAsync(reportDefinition.Id);
            if (existingReport == null)
            {
                return null;
            }

            // Update properties
            existingReport.Name = reportDefinition.Name;
            existingReport.Description = reportDefinition.Description;
            existingReport.Category = reportDefinition.Category;
            existingReport.QueryDefinition = reportDefinition.QueryDefinition;
            existingReport.Parameters = reportDefinition.Parameters;
            existingReport.IsEnabled = reportDefinition.IsEnabled;
            existingReport.LastModifiedAt = DateTime.Now;
            existingReport.LastModifiedById = reportDefinition.LastModifiedById;

            await _context.SaveChangesAsync();
            return existingReport;
        }

        public async Task<bool> DeleteReportDefinitionAsync(int id)
        {
            var reportDefinition = await _context.ReportDefinitions.FindAsync(id);
            if (reportDefinition == null)
            {
                return false;
            }

            // Check if it's a built-in report
            if (reportDefinition.IsBuiltIn)
            {
                return false;
            }

            _context.ReportDefinitions.Remove(reportDefinition);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Report Parameters

        public async Task<List<ReportParameter>> GetParametersForReportAsync(int reportDefinitionId)
        {
            return await _context.ReportParameters
                .Where(p => p.ReportDefinitionId == reportDefinitionId)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }

        public async Task<ReportParameter> CreateReportParameterAsync(ReportParameter parameter)
        {
            _context.ReportParameters.Add(parameter);
            await _context.SaveChangesAsync();
            return parameter;
        }

        public async Task<ReportParameter?> UpdateReportParameterAsync(ReportParameter parameter)
        {
            var existingParameter = await _context.ReportParameters.FindAsync(parameter.Id);
            if (existingParameter == null)
            {
                return null;
            }

            existingParameter.Name = parameter.Name;
            existingParameter.Label = parameter.Label;
            existingParameter.Type = parameter.Type;
            existingParameter.DefaultValue = parameter.DefaultValue;
            existingParameter.ValidationRules = parameter.ValidationRules;
            existingParameter.Options = parameter.Options;
            existingParameter.IsRequired = parameter.IsRequired;
            existingParameter.SortOrder = parameter.SortOrder;

            await _context.SaveChangesAsync();
            return existingParameter;
        }

        public async Task<bool> DeleteReportParameterAsync(int id)
        {
            var parameter = await _context.ReportParameters.FindAsync(id);
            if (parameter == null)
            {
                return false;
            }

            _context.ReportParameters.Remove(parameter);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Report Generation

        public async Task<Dictionary<string, object>> GenerateReportAsync(int reportDefinitionId, Dictionary<string, string>? parameters = null)
        {
            var reportDefinition = await _context.ReportDefinitions.FindAsync(reportDefinitionId);
            if (reportDefinition == null)
            {
                throw new ArgumentException("Report definition not found", nameof(reportDefinitionId));
            }

            // Based on the report category, call the appropriate report generator
            switch (reportDefinition.Category)
            {
                case ReportCategory.Finance:
                    var startDate = parameters != null && parameters.ContainsKey("startDate") 
                        ? DateTime.Parse(parameters["startDate"]) 
                        : DateTime.Today.AddMonths(-1);
                    var endDate = parameters != null && parameters.ContainsKey("endDate") 
                        ? DateTime.Parse(parameters["endDate"]) 
                        : DateTime.Today;
                    return await GenerateBillsAndPaymentsReportAsync(startDate, endDate);

                case ReportCategory.ServiceRequests:
                    startDate = parameters != null && parameters.ContainsKey("startDate") 
                        ? DateTime.Parse(parameters["startDate"]) 
                        : DateTime.Today.AddMonths(-1);
                    endDate = parameters != null && parameters.ContainsKey("endDate") 
                        ? DateTime.Parse(parameters["endDate"]) 
                        : DateTime.Today;
                    ServiceRequestStatus? status = null;
                    if (parameters != null && parameters.ContainsKey("status") && Enum.TryParse<ServiceRequestStatus>(parameters["status"], out var statusValue))
                    {
                        status = statusValue;
                    }
                    return await GenerateServiceRequestsReportAsync(startDate, endDate, status);

                case ReportCategory.CommunityEngagement:
                    startDate = parameters != null && parameters.ContainsKey("startDate") 
                        ? DateTime.Parse(parameters["startDate"]) 
                        : DateTime.Today.AddMonths(-1);
                    endDate = parameters != null && parameters.ContainsKey("endDate") 
                        ? DateTime.Parse(parameters["endDate"]) 
                        : DateTime.Today;
                    return await GenerateCommunityEngagementReportAsync(startDate, endDate);

                default:
                    throw new NotImplementedException($"Report generation for category {reportDefinition.Category} is not implemented");
            }
        }

        public async Task<SavedReport> SaveReportResultAsync(SavedReport report)
        {
            _context.SavedReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        #endregion

        #region Saved Reports

        public async Task<List<SavedReport>> GetSavedReportsAsync(int? reportDefinitionId = null)
        {
            var query = _context.SavedReports.AsQueryable();
            
            if (reportDefinitionId.HasValue)
            {
                query = query.Where(r => r.ReportDefinitionId == reportDefinitionId.Value);
            }
            
            return await query
                .Include(r => r.ReportDefinition)
                .Include(r => r.GeneratedBy)
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync();
        }

        public async Task<SavedReport?> GetSavedReportByIdAsync(int id)
        {
            return await _context.SavedReports
                .Include(r => r.ReportDefinition)
                .Include(r => r.GeneratedBy)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> DeleteSavedReportAsync(int id)
        {
            var report = await _context.SavedReports.FindAsync(id);
            if (report == null)
            {
                return false;
            }

            _context.SavedReports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Dashboard Widgets

        public async Task<List<DashboardWidget>> GetWidgetsForDashboardAsync(UserRole userRole)
        {
            int roleFlag;
            switch (userRole)
            {
                case UserRole.Administrator:
                    roleFlag = 1;
                    break;
                case UserRole.Staff:
                    roleFlag = 2;
                    break;
                case UserRole.Homeowner:
                    roleFlag = 4;
                    break;
                default:
                    roleFlag = 0;
                    break;
            }

            return await _context.DashboardWidgets
                .Where(w => w.IsEnabled && (w.VisibleToRoles & roleFlag) != 0)
                .OrderBy(w => w.SortOrder)
                .ToListAsync();
        }

        public async Task<DashboardWidget?> GetWidgetByIdAsync(int id)
        {
            return await _context.DashboardWidgets.FindAsync(id);
        }

        public async Task<DashboardWidget> CreateWidgetAsync(DashboardWidget widget)
        {
            _context.DashboardWidgets.Add(widget);
            await _context.SaveChangesAsync();
            return widget;
        }

        public async Task<DashboardWidget?> UpdateWidgetAsync(DashboardWidget widget)
        {
            var existingWidget = await _context.DashboardWidgets.FindAsync(widget.Id);
            if (existingWidget == null)
            {
                return null;
            }

            existingWidget.Title = widget.Title;
            existingWidget.Description = widget.Description;
            existingWidget.Type = widget.Type;
            existingWidget.Size = widget.Size;
            existingWidget.SortOrder = widget.SortOrder;
            existingWidget.Configuration = widget.Configuration;
            existingWidget.ReportDefinitionId = widget.ReportDefinitionId;
            existingWidget.IsEnabled = widget.IsEnabled;
            existingWidget.VisibleToRoles = widget.VisibleToRoles;

            await _context.SaveChangesAsync();
            return existingWidget;
        }

        public async Task<bool> DeleteWidgetAsync(int id)
        {
            var widget = await _context.DashboardWidgets.FindAsync(id);
            if (widget == null)
            {
                return false;
            }

            _context.DashboardWidgets.Remove(widget);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Pre-defined Reports

        public async Task<Dictionary<string, object>> GenerateBillsAndPaymentsReportAsync(DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, object>();
            
            // 1. Total bills issued in period
            var billsInPeriod = await _context.Bills
                .Where(b => b.IssueDate >= startDate && b.IssueDate <= endDate)
                .ToListAsync();
            
            var totalBillsAmount = billsInPeriod.Sum(b => b.Amount);
            var billsByType = billsInPeriod
                .GroupBy(b => b.Type)
                .Select(g => new { Type = g.Key, Count = g.Count(), Amount = g.Sum(b => b.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();
            
            // 2. Total payments received in period
            var paymentsInPeriod = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .ToListAsync();
            
            var totalPaymentsAmount = paymentsInPeriod.Sum(p => p.Amount);
            var paymentsByMethod = paymentsInPeriod
                .GroupBy(p => p.PaymentMethod != null ? p.PaymentMethod.Name : "No Method")
                .Select(g => new { Method = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();
            
            // 3. Overdue bills as of end date
            var overdueBills = await _context.Bills
                .Where(b => b.DueDate < endDate && b.Status != "Paid")
                .ToListAsync();
            
            var totalOverdueAmount = overdueBills.Sum(b => b.RemainingAmount);
            var overdueByDuration = overdueBills
                .GroupBy(b => (endDate - b.DueDate).Days switch
                {
                    < 30 => "< 30 days",
                    < 60 => "30-60 days",
                    < 90 => "60-90 days",
                    _ => "> 90 days"
                })
                .Select(g => new { Duration = g.Key, Count = g.Count(), Amount = g.Sum(b => b.RemainingAmount) })
                .OrderBy(x => x.Duration)
                .ToList();
            
            // 4. Monthly revenue trend
            var paymentsForTrend = await _context.Payments
                .Where(p => p.PaymentDate >= startDate.AddMonths(-6) && p.PaymentDate <= endDate && p.Status == "Completed")
                .Select(p => new 
                { 
                    Year = p.PaymentDate.Year, 
                    Month = p.PaymentDate.Month,
                    Amount = p.Amount
                })
                .ToListAsync();

            var monthlyTrend = paymentsForTrend
                .GroupBy(p => new { p.Year, p.Month })
                .Select(g => new 
                { 
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}", 
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Period)
                .ToList();
            
            // Populate result dictionary
            result["ReportTitle"] = "Bills and Payments Report";
            result["StartDate"] = startDate;
            result["EndDate"] = endDate;
            result["TotalBillsIssued"] = billsInPeriod.Count;
            result["TotalBillsAmount"] = totalBillsAmount;
            result["BillsByType"] = billsByType;
            result["TotalPaymentsReceived"] = paymentsInPeriod.Count;
            result["TotalPaymentsAmount"] = totalPaymentsAmount;
            result["PaymentsByMethod"] = paymentsByMethod;
            result["TotalOverdueBills"] = overdueBills.Count;
            result["TotalOverdueAmount"] = totalOverdueAmount;
            result["OverdueByDuration"] = overdueByDuration;
            result["MonthlyTrend"] = monthlyTrend;
            
            return result;
        }

        public async Task<Dictionary<string, object>> GenerateServiceRequestsReportAsync(DateTime startDate, DateTime endDate, ServiceRequestStatus? status = null)
        {
            var result = new Dictionary<string, object>();
            
            // Build the query
            var requestsData = await _context.ServiceRequests
                .Where(sr => sr.SubmissionDate >= startDate && sr.SubmissionDate <= endDate)
                .Where(sr => !status.HasValue || sr.Status == status.Value)
                .ToListAsync();

            // 1. Count by status
            var countByStatus = requestsData
                .GroupBy(sr => sr.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .OrderBy(x => x.Status)
                .ToList();

            // 2. Count by request type
            var countByType = requestsData
                .GroupBy(sr => sr.RequestType)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // 3. Count by priority
            var countByPriority = requestsData
                .GroupBy(sr => sr.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .OrderBy(x => x.Priority)
                .ToList();
            
            // 4. Resolution time statistics
            var completedRequests = requestsData
                .Where(sr => sr.Status == ServiceRequestStatus.Completed && sr.CompletionDate.HasValue)
                .ToList();
                
            double avgResolutionHours = 0;
            if (completedRequests.Any())
            {
                avgResolutionHours = completedRequests
                    .Average(sr => (sr.CompletionDate!.Value - sr.SubmissionDate).TotalHours);
            }
            
            // 5. Resolution time by type
            var resolutionByType = completedRequests
                .GroupBy(sr => sr.RequestType)
                .Select(g => new 
                { 
                    Type = g.Key.ToString(), 
                    AverageHours = g.Average(sr => (sr.CompletionDate!.Value - sr.SubmissionDate).TotalHours),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            // 6. Trend over time (daily/weekly)
            var dailyTrend = requestsData
                .GroupBy(sr => sr.SubmissionDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            
            // Populate result dictionary
            result["ReportTitle"] = "Service Requests Report";
            result["StartDate"] = startDate;
            result["EndDate"] = endDate;
            result["TotalRequests"] = requestsData.Count;
            result["CountByStatus"] = countByStatus;
            result["CountByType"] = countByType;
            result["CountByPriority"] = countByPriority;
            result["CompletedRequests"] = completedRequests.Count;
            result["AverageResolutionHours"] = avgResolutionHours;
            result["ResolutionByType"] = resolutionByType;
            result["DailyTrend"] = dailyTrend;
            
            return result;
        }

        public async Task<Dictionary<string, object>> GenerateCommunityEngagementReportAsync(DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, object>();
            
            // 1. Forum activity
            var forumPosts = await _context.ForumPosts
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync();
                
            var forumTopics = await _context.ForumTopics
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();
                
            var forumReactions = await _context.ForumReactions
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();
            
            // 2. Most active categories
            var postsWithCategories = await _context.ForumPosts
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .Include(p => p.Topic)
                .ThenInclude(t => t.Category)
                .Select(p => new 
                {
                    CategoryName = p.Topic.Category.Name
                })
                .ToListAsync();

            var activeCategories = postsWithCategories
                .GroupBy(p => p.CategoryName)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            
            // 3. Top contributors
            var postsWithCreators = await _context.ForumPosts
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .Include(p => p.CreatedBy)
                .Select(p => new 
                {
                    UserId = p.CreatedById,
                    Username = p.CreatedBy.Username
                })
                .ToListAsync();

            var topContributors = postsWithCreators
                .GroupBy(p => new { p.UserId, p.Username })
                .Select(g => new 
                { 
                    UserId = g.Key.UserId, 
                    Username = g.Key.Username, 
                    PostCount = g.Count() 
                })
                .OrderByDescending(x => x.PostCount)
                .Take(10)
                .ToList();
            
            // 4. Event information
            var events = await _context.Events
                .Where(e => e.StartDate >= startDate && e.StartDate <= endDate)
                .ToListAsync();
                
            var totalEvents = events.Count;
            
            // 5. Daily/weekly engagement trend
            var dailyForumActivity = forumPosts
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();
            
            // Populate result dictionary
            result["ReportTitle"] = "Community Engagement Report";
            result["StartDate"] = startDate;
            result["EndDate"] = endDate;
            result["TotalForumPosts"] = forumPosts.Count;
            result["TotalForumTopics"] = forumTopics.Count;
            result["TotalForumReactions"] = forumReactions.Count;
            result["ActiveCategories"] = activeCategories;
            result["TopContributors"] = topContributors;
            result["TotalEvents"] = totalEvents;
            result["DailyForumActivity"] = dailyForumActivity;
            
            return result;
        }

        #endregion
    }
} 