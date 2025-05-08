using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;

namespace HomeownersSubdivision.Services
{
    public interface IReportService
    {
        // Report Definition Management
        Task<List<ReportDefinition>> GetAllReportDefinitionsAsync();
        Task<ReportDefinition?> GetReportDefinitionByIdAsync(int id);
        Task<ReportDefinition> CreateReportDefinitionAsync(ReportDefinition reportDefinition);
        Task<ReportDefinition?> UpdateReportDefinitionAsync(ReportDefinition reportDefinition);
        Task<bool> DeleteReportDefinitionAsync(int id);
        
        // Report Parameters
        Task<List<ReportParameter>> GetParametersForReportAsync(int reportDefinitionId);
        Task<ReportParameter> CreateReportParameterAsync(ReportParameter parameter);
        Task<ReportParameter?> UpdateReportParameterAsync(ReportParameter parameter);
        Task<bool> DeleteReportParameterAsync(int id);
        
        // Report Generation
        Task<Dictionary<string, object>> GenerateReportAsync(int reportDefinitionId, Dictionary<string, string>? parameters = null);
        Task<SavedReport> SaveReportResultAsync(SavedReport report);
        
        // Saved Reports
        Task<List<SavedReport>> GetSavedReportsAsync(int? reportDefinitionId = null);
        Task<SavedReport?> GetSavedReportByIdAsync(int id);
        Task<bool> DeleteSavedReportAsync(int id);
        
        // Dashboard Widgets
        Task<List<DashboardWidget>> GetWidgetsForDashboardAsync(UserRole userRole);
        Task<DashboardWidget?> GetWidgetByIdAsync(int id);
        Task<DashboardWidget> CreateWidgetAsync(DashboardWidget widget);
        Task<DashboardWidget?> UpdateWidgetAsync(DashboardWidget widget);
        Task<bool> DeleteWidgetAsync(int id);
        
        // Pre-defined Reports
        Task<Dictionary<string, object>> GenerateBillsAndPaymentsReportAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, object>> GenerateServiceRequestsReportAsync(DateTime startDate, DateTime endDate, ServiceRequestStatus? status = null);
        Task<Dictionary<string, object>> GenerateCommunityEngagementReportAsync(DateTime startDate, DateTime endDate);
    }
} 