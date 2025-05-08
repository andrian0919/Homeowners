using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Models.Analytics;
using HomeownersSubdivision.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, IReportService reportService, ILogger<ReportsController> logger)
        {
            _context = context;
            _reportService = reportService;
            _logger = logger;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            // Only administrators can access reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var reports = await _reportService.GetAllReportDefinitionsAsync();
            return View(reports);
        }
        
        // GET: Reports/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Get the user role from session
            var userRoleString = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRoleString) || !Enum.TryParse<UserRole>(userRoleString, out var userRole))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Only administrators and staff can access the dashboard
            if (userRole != UserRole.Administrator && userRole != UserRole.Staff)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Set viewdata for dynamic admin background
            ViewData["UseAdminBackground"] = true;
            
            // Get widgets for the dashboard based on user role
            var widgets = await _reportService.GetWidgetsForDashboardAsync(userRole);
            
            // Get real data for the dashboard
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30);
            
            // Get financial data
            var financialData = await _reportService.GenerateBillsAndPaymentsReportAsync(startDate, endDate);
            ViewBag.FinancialData = financialData;
            
            // Get service request data
            var serviceRequestData = await _reportService.GenerateServiceRequestsReportAsync(startDate, endDate);
            ViewBag.ServiceRequestData = serviceRequestData;
            
            // Get community engagement data
            var communityData = await _reportService.GenerateCommunityEngagementReportAsync(startDate, endDate);
            ViewBag.CommunityData = communityData;
            
            // Get facility usage data
            var facilityReservations = await _context.FacilityReservations
                .Where(r => r.ReservationDate >= startDate && r.ReservationDate <= endDate)
                .GroupBy(r => r.Facility.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count(),
                    UsagePercentage = (double)g.Count() / 30 * 100 // Assuming 30 days in a month
                })
                .ToListAsync();
            
            ViewBag.TotalReservations = facilityReservations.Sum(f => f.Count);
            ViewBag.FacilityUsage = facilityReservations;
            
            return View(widgets);
        }
        
        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Only administrators can access report details
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var reportDefinition = await _reportService.GetReportDefinitionByIdAsync(id.Value);
            if (reportDefinition == null)
            {
                return NotFound();
            }
            
            // Get parameters for this report
            var parameters = await _reportService.GetParametersForReportAsync(id.Value);
            ViewBag.Parameters = parameters;

            return View(reportDefinition);
        }
        
        // GET: Reports/Generate/5
        public async Task<IActionResult> Generate(int? id)
        {
            // Only administrators can generate reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var reportDefinition = await _reportService.GetReportDefinitionByIdAsync(id.Value);
            if (reportDefinition == null)
            {
                return NotFound();
            }
            
            // Get parameters for this report
            var parameters = await _reportService.GetParametersForReportAsync(id.Value);
            
            ViewBag.ReportDefinition = reportDefinition;
            return View(parameters);
        }
        
        // POST: Reports/Generate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int id, Dictionary<string, string> parameters)
        {
            // Only administrators can generate reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var reportDefinition = await _reportService.GetReportDefinitionByIdAsync(id);
            if (reportDefinition == null)
            {
                TempData["ErrorMessage"] = $"Report definition with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                // Log parameters received
                _logger.LogInformation($"Generating report {reportDefinition.Name} with parameters: {JsonSerializer.Serialize(parameters)}");
                
                // Validate parameters
                var reportParams = await _reportService.GetParametersForReportAsync(id);
                foreach (var param in reportParams.Where(p => p.IsRequired))
                {
                    if (!parameters.ContainsKey(param.Name) || string.IsNullOrWhiteSpace(parameters[param.Name]))
                    {
                        ModelState.AddModelError(param.Name, $"The {param.Label} field is required.");
                    }
                }
                
                if (!ModelState.IsValid)
                {
                    ViewBag.ReportDefinition = reportDefinition;
                    return View(reportParams);
                }
                    
                // Generate the report using the service
                var result = await _reportService.GenerateReportAsync(id, parameters);
                
                if (result == null || !result.Any())
                {
                    TempData["WarningMessage"] = "The report was generated but returned no data.";
                }
                
                // Store the result in TempData for the Results view
                TempData["ReportResult"] = JsonSerializer.Serialize(result);
                TempData["ReportName"] = reportDefinition.Name;
                
                // Create a SavedReport entry - make sure the user ID is correctly assigned
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    // Log this issue
                    _logger.LogWarning("User ID not found in session. Report saved without user attribution.");
                }

                // Ensure we have properly formatted dates for parameters
                DateTime? startDate = null;
                DateTime? endDate = null;

                if (parameters.ContainsKey("startDate") && !string.IsNullOrWhiteSpace(parameters["startDate"]))
                {
                    if (DateTime.TryParse(parameters["startDate"], out var parsedStartDate))
                    {
                        startDate = parsedStartDate;
                    }
                }

                if (parameters.ContainsKey("endDate") && !string.IsNullOrWhiteSpace(parameters["endDate"]))
                {
                    if (DateTime.TryParse(parameters["endDate"], out var parsedEndDate))
                    {
                        endDate = parsedEndDate;
                    }
                }

                // Convert result to proper JSON to ensure it's stored correctly
                string resultData;
                try 
                {
                    resultData = JsonSerializer.Serialize(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error serializing report result");
                    resultData = JsonSerializer.Serialize(new Dictionary<string, object> { 
                        { "Error", "Failed to serialize report data" } 
                    });
                }

                var parametersJson = JsonSerializer.Serialize(parameters);

                var savedReport = new SavedReport
                {
                    ReportDefinitionId = id,
                    Name = $"{reportDefinition.Name} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                    GeneratedAt = DateTime.Now,
                    GeneratedById = userId,
                    ResultData = resultData,
                    Parameters = parametersJson,
                    StartDate = startDate,
                    EndDate = endDate
                };
                
                var savedReportResult = await _reportService.SaveReportResultAsync(savedReport);
                
                // Check if the report was actually saved by verifying if it has an ID
                if (savedReportResult.Id <= 0)
                {
                    TempData["WarningMessage"] = "Report was generated but could not be saved permanently.";
                    return RedirectToAction(nameof(Results));
                }
                
                // Redirect to the results view for the saved report
                TempData["SuccessMessage"] = "Report generated and saved successfully.";
                return RedirectToAction(nameof(Results), new { id = savedReportResult.Id });
            }
            catch (Exception ex)
            {
                // Log the error and return to the form with an error message
                _logger.LogError(ex, $"Error generating report: {ex.Message}");
                ModelState.AddModelError("", $"Error generating report: {ex.Message}");
                ViewBag.ReportDefinition = reportDefinition;
                var reportParameters = await _reportService.GetParametersForReportAsync(id);
                return View(reportParameters);
            }
        }
        
        // GET: Reports/Results/5
        public async Task<IActionResult> Results(int? id)
        {
            // Only administrators can view report results
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                // Check if we have results in TempData
                if (TempData["ReportResult"] != null && TempData["ReportName"] != null)
                {
                    try
                    {
                        var resultString = TempData["ReportResult"].ToString();
                        
                        if (string.IsNullOrEmpty(resultString))
                        {
                            TempData["ErrorMessage"] = "Report result data is empty.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(resultString);
                        if (result == null || !result.Any())
                        {
                            TempData["ErrorMessage"] = "Report generated empty results.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        ViewBag.ReportName = TempData["ReportName"].ToString();
                        return View(result);
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Error processing report results: {ex.Message}";
                        return RedirectToAction(nameof(Index));
                    }
                }
                
                TempData["ErrorMessage"] = "No report results found.";
                return RedirectToAction(nameof(Index));
            }
            
            var savedReport = await _reportService.GetSavedReportByIdAsync(id.Value);
            if (savedReport == null)
            {
                TempData["ErrorMessage"] = $"Saved report with ID {id} not found.";
                return RedirectToAction(nameof(SavedReports));
            }
            
            try
            {
                if (string.IsNullOrEmpty(savedReport.ResultData))
                {
                    TempData["ErrorMessage"] = "Saved report contains no data.";
                    return RedirectToAction(nameof(SavedReports));
                }
                
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(savedReport.ResultData);
                if (result == null || !result.Any())
                {
                    TempData["ErrorMessage"] = "Saved report contains empty data.";
                    return RedirectToAction(nameof(SavedReports));
                }
                
                ViewBag.SavedReport = savedReport;
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading saved report: {ex.Message}";
                return RedirectToAction(nameof(SavedReports));
            }
        }
        
        // GET: Reports/SavedReports
        public async Task<IActionResult> SavedReports()
        {
            // Only administrators can view saved reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var reports = await _reportService.GetSavedReportsAsync();
            return View(reports);
        }
        
        // GET: Reports/DeleteSavedReport/5
        public async Task<IActionResult> DeleteSavedReport(int? id)
        {
            // Only administrators can delete saved reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                return NotFound();
            }
            
            var report = await _reportService.GetSavedReportByIdAsync(id.Value);
            if (report == null)
            {
                return NotFound();
            }
            
            return View(report);
        }
        
        // POST: Reports/DeleteSavedReport/5
        [HttpPost, ActionName("DeleteSavedReport")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSavedReportConfirmed(int id)
        {
            // Only administrators can delete saved reports
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            await _reportService.DeleteSavedReportAsync(id);
            return RedirectToAction(nameof(SavedReports));
        }
        
        // Quick reports - financial summary
        public async Task<IActionResult> FinancialSummary()
        {
            // Only administrators can view financial summary
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Default to last 30 days
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30);
            
            var result = await _reportService.GenerateBillsAndPaymentsReportAsync(startDate, endDate);
            ViewBag.ReportName = "Financial Summary (Last 30 Days)";
            return View("Results", result);
        }
        
        // Quick reports - service requests summary
        public async Task<IActionResult> ServiceRequestsSummary()
        {
            // Only administrators can view service requests summary
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Default to last 30 days
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30);
            
            var result = await _reportService.GenerateServiceRequestsReportAsync(startDate, endDate);
            ViewBag.ReportName = "Service Requests Summary (Last 30 Days)";
            return View("Results", result);
        }
        
        // Quick reports - community engagement summary
        public async Task<IActionResult> CommunityEngagementSummary()
        {
            // Only administrators can view community engagement summary
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Default to last 30 days
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-30);
            
            var result = await _reportService.GenerateCommunityEngagementReportAsync(startDate, endDate);
            ViewBag.ReportName = "Community Engagement Summary (Last 30 Days)";
            return View("Results", result);
        }
    }
} 