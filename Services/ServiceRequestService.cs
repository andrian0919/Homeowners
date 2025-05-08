using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Services
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceRequestService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;

        public ServiceRequestService(
            ApplicationDbContext context,
            ILogger<ServiceRequestService> logger,
            IWebHostEnvironment webHostEnvironment,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }

        public async Task<List<ServiceRequest>> GetAllServiceRequestsAsync(
            ServiceRequestFilterViewModel? filter = null,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                IQueryable<ServiceRequest> query = _context.ServiceRequests
                    .Include(s => s.Homeowner)
                    .Include(s => s.AssignedTo)
                    .AsQueryable();

                // Apply filters if provided
                if (filter != null)
                {
                    if (filter.Status.HasValue)
                    {
                        query = query.Where(s => s.Status == filter.Status.Value);
                    }

                    if (filter.Type.HasValue)
                    {
                        query = query.Where(s => s.RequestType == filter.Type.Value);
                    }

                    if (filter.Priority.HasValue)
                    {
                        query = query.Where(s => s.Priority == filter.Priority.Value);
                    }

                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(s => s.SubmissionDate >= filter.FromDate.Value);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        // Add one day to include the full end date
                        var endDate = filter.ToDate.Value.AddDays(1);
                        query = query.Where(s => s.SubmissionDate < endDate);
                    }

                    if (filter.MyRequestsOnly && filter is { } contextFilter && contextFilter.GetType()
                        .GetProperty("CurrentUserId")?.GetValue(contextFilter) is int userId)
                    {
                        // Check if user is a homeowner or staff member
                        var user = await _context.Users.Include(u => u.Homeowner)
                            .FirstOrDefaultAsync(u => u.Id == userId);
                        
                        if (user.Homeowner != null)
                        {
                            // If homeowner, show their requests
                            query = query.Where(s => s.HomeownerId == user.Homeowner.Id);
                        }
                        else if (user.Role == UserRole.Staff || user.Role == UserRole.Administrator)
                        {
                            // If staff, show assigned requests
                            query = query.Where(s => s.AssignedToId == userId);
                        }
                    }
                }

                // Apply pagination
                return await query
                    .OrderByDescending(s => s.SubmissionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service requests");
                throw;
            }
        }

        public async Task<int> GetServiceRequestsCountAsync(ServiceRequestFilterViewModel? filter = null)
        {
            try
            {
                IQueryable<ServiceRequest> query = _context.ServiceRequests;

                // Apply filters if provided
                if (filter != null)
                {
                    if (filter.Status.HasValue)
                    {
                        query = query.Where(s => s.Status == filter.Status.Value);
                    }

                    if (filter.Type.HasValue)
                    {
                        query = query.Where(s => s.RequestType == filter.Type.Value);
                    }

                    if (filter.Priority.HasValue)
                    {
                        query = query.Where(s => s.Priority == filter.Priority.Value);
                    }

                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(s => s.SubmissionDate >= filter.FromDate.Value);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        // Add one day to include the full end date
                        var endDate = filter.ToDate.Value.AddDays(1);
                        query = query.Where(s => s.SubmissionDate < endDate);
                    }

                    if (filter.MyRequestsOnly && filter is { } contextFilter && contextFilter.GetType()
                        .GetProperty("CurrentUserId")?.GetValue(contextFilter) is int userId)
                    {
                        // Check if user is a homeowner or staff member
                        var user = await _context.Users.Include(u => u.Homeowner)
                            .FirstOrDefaultAsync(u => u.Id == userId);
                        
                        if (user.Homeowner != null)
                        {
                            // If homeowner, show their requests
                            query = query.Where(s => s.HomeownerId == user.Homeowner.Id);
                        }
                        else if (user.Role == UserRole.Staff || user.Role == UserRole.Administrator)
                        {
                            // If staff, show assigned requests
                            query = query.Where(s => s.AssignedToId == userId);
                        }
                    }
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service requests count");
                throw;
            }
        }

        public async Task<ServiceRequest> GetServiceRequestByIdAsync(int id)
        {
            try
            {
                return await _context.ServiceRequests
                    .Include(s => s.Homeowner)
                    .Include(s => s.AssignedTo)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service request with ID {Id}", id);
                throw;
            }
        }

        public async Task<List<ServiceRequest>> GetServiceRequestsByHomeownerIdAsync(int homeownerId)
        {
            try
            {
                return await _context.ServiceRequests
                    .Where(s => s.HomeownerId == homeownerId)
                    .Include(s => s.AssignedTo)
                    .OrderByDescending(s => s.SubmissionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service requests for homeowner with ID {HomeownerId}", homeownerId);
                throw;
            }
        }

        public async Task<ServiceRequest> CreateServiceRequestAsync(
            ServiceRequest request, 
            IFormFile? image = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                // Save image if provided
                if (image != null)
                {
                    await SaveServiceRequestImageAsync(request.Id, image);
                    
                    // Update the request with the image URL
                    request.ImageUrl = $"/uploads/service-requests/{request.Id}/{image.FileName}";
                    await _context.SaveChangesAsync();
                }

                // Send notification to admin and staff
                await _notificationService.CreateNotificationsForRoleAsync(
                    role: UserRole.Administrator,
                    title: "New Service Request",
                    message: $"A new {request.RequestType} request has been submitted: {request.Title}",
                    type: NotificationType.ServiceRequest
                );

                await _notificationService.CreateNotificationsForRoleAsync(
                    role: UserRole.Staff,
                    title: "New Service Request",
                    message: $"A new {request.RequestType} request has been submitted: {request.Title}",
                    type: NotificationType.ServiceRequest
                );

                await transaction.CommitAsync();
                return request;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating service request");
                throw;
            }
        }

        public async Task<ServiceRequest> UpdateServiceRequestAsync(
            ServiceRequest request, 
            string? staffNotes = null,
            IFormFile? newImage = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingRequest = await _context.ServiceRequests.FindAsync(request.Id);
                if (existingRequest == null)
                {
                    return null;
                }

                // Track original status for notification purposes
                var originalStatus = existingRequest.Status;
                
                // Update properties
                existingRequest.Title = request.Title;
                existingRequest.Description = request.Description;
                existingRequest.Priority = request.Priority;
                existingRequest.RequestType = request.RequestType;
                existingRequest.Status = request.Status;
                existingRequest.AssignedToId = request.AssignedToId;
                existingRequest.LastUpdated = DateTime.Now;
                
                // Update completion date if status is completed
                if (request.Status == ServiceRequestStatus.Completed && existingRequest.CompletionDate == null)
                {
                    existingRequest.CompletionDate = DateTime.Now;
                }
                else if (request.Status != ServiceRequestStatus.Completed)
                {
                    existingRequest.CompletionDate = null;
                }

                // Add staff notes if provided
                if (!string.IsNullOrEmpty(staffNotes))
                {
                    existingRequest.StaffNotes = string.IsNullOrEmpty(existingRequest.StaffNotes)
                        ? staffNotes
                        : existingRequest.StaffNotes + "\n\n" + staffNotes;
                }

                // Process new image if provided
                if (newImage != null)
                {
                    await SaveServiceRequestImageAsync(existingRequest.Id, newImage);
                    existingRequest.ImageUrl = $"/uploads/service-requests/{existingRequest.Id}/{newImage.FileName}";
                }

                await _context.SaveChangesAsync();

                // Record the update
                var update = new ServiceRequestUpdate
                {
                    ServiceRequestId = request.Id,
                    UpdatedAt = DateTime.Now,
                    UpdatedById = request.AssignedToId ?? 0,
                    Notes = staffNotes,
                    OldStatus = originalStatus,
                    NewStatus = request.Status
                };

                _context.Set<ServiceRequestUpdate>().Add(update);
                await _context.SaveChangesAsync();

                // Send notification if status changed
                if (originalStatus != request.Status)
                {
                    // Get homeowner's user ID
                    var homeowner = await _context.Homeowners
                        .FirstOrDefaultAsync(h => h.Id == existingRequest.HomeownerId);

                    if (homeowner != null)
                    {
                        var homeownerUser = await _context.Users.FirstOrDefaultAsync(u => u.HomeownerId == homeowner.Id);
                        if (homeownerUser != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                                homeownerUser.Id,
                                "Service Request Status Update",
                                $"Your service request \"{existingRequest.Title}\" status has been updated to {existingRequest.Status}.",
                                NotificationType.ServiceRequest
                        );
                        }
                    }
                }

                await transaction.CommitAsync();
                return existingRequest;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating service request with ID {Id}", request.Id);
                throw;
            }
        }

        public async Task<bool> DeleteServiceRequestAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.ServiceRequests.FindAsync(id);
                if (request == null)
                {
                    return false;
                }

                // Delete associated updates
                var updates = await _context.Set<ServiceRequestUpdate>()
                    .Where(u => u.ServiceRequestId == id)
                    .ToListAsync();
                
                _context.Set<ServiceRequestUpdate>().RemoveRange(updates);

                // Delete the request
                _context.ServiceRequests.Remove(request);
                await _context.SaveChangesAsync();

                // Delete the image folder if it exists
                var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "service-requests", id.ToString());
                if (Directory.Exists(imageFolder))
                {
                    Directory.Delete(imageFolder, true);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting service request with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> AssignServiceRequestAsync(int requestId, int staffId)
        {
            try
            {
                var request = await _context.ServiceRequests.FindAsync(requestId);
                if (request == null)
                {
                    return false;
                }

                // Check if the staff member exists
                var staff = await _context.Users.FindAsync(staffId);
                if (staff == null || (staff.Role != UserRole.Staff && staff.Role != UserRole.Administrator))
                {
                    return false;
                }

                request.AssignedToId = staffId;
                request.LastUpdated = DateTime.Now;
                
                // If status is new, change to in progress
                if (request.Status == ServiceRequestStatus.New)
                {
                    request.Status = ServiceRequestStatus.InProgress;
                }

                await _context.SaveChangesAsync();

                // Record the update
                var update = new ServiceRequestUpdate
                {
                    ServiceRequestId = requestId,
                    UpdatedAt = DateTime.Now,
                    UpdatedById = staffId,
                    Notes = $"Request assigned to {staff.FirstName} {staff.LastName}",
                    OldStatus = request.Status,
                    NewStatus = request.Status
                };

                _context.Set<ServiceRequestUpdate>().Add(update);
                await _context.SaveChangesAsync();

                // Send notification to assigned staff
                await _notificationService.CreateNotificationAsync(
                    staffId,
                    "New Service Request Assignment",
                    $"You have been assigned to service request \"{request.Title}\".",
                    NotificationType.Assignment
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning service request {RequestId} to staff {StaffId}", requestId, staffId);
                throw;
            }
        }

        public async Task<bool> ChangeServiceRequestStatusAsync(
            int requestId, 
            ServiceRequestStatus newStatus, 
            string? notes = null)
        {
            try
            {
                var request = await _context.ServiceRequests.FindAsync(requestId);
                if (request == null)
                {
                    return false;
                }

                var originalStatus = request.Status;
                request.Status = newStatus;
                request.LastUpdated = DateTime.Now;

                // Update completion date if status is completed
                if (newStatus == ServiceRequestStatus.Completed && request.CompletionDate == null)
                {
                    request.CompletionDate = DateTime.Now;
                }
                else if (newStatus != ServiceRequestStatus.Completed)
                {
                    request.CompletionDate = null;
                }

                // Add notes if provided
                if (!string.IsNullOrEmpty(notes))
                {
                    request.StaffNotes = string.IsNullOrEmpty(request.StaffNotes)
                        ? notes
                        : request.StaffNotes + "\n\n" + notes;
                }

                await _context.SaveChangesAsync();

                // Record the update
                var update = new ServiceRequestUpdate
                {
                    ServiceRequestId = requestId,
                    UpdatedAt = DateTime.Now,
                    UpdatedById = request.AssignedToId ?? 0,
                    Notes = notes,
                    OldStatus = originalStatus,
                    NewStatus = newStatus
                };

                _context.Set<ServiceRequestUpdate>().Add(update);
                await _context.SaveChangesAsync();

                // Send notification to homeowner
                var homeowner = await _context.Homeowners
                    .FirstOrDefaultAsync(h => h.Id == request.HomeownerId);

                if (homeowner != null)
                {
                    var homeownerUser = await _context.Users.FirstOrDefaultAsync(u => u.HomeownerId == homeowner.Id);
                    if (homeownerUser != null)
                {
                    await _notificationService.CreateNotificationAsync(
                            homeownerUser.Id,
                            "Service Request Status Update",
                            $"Your service request \"{request.Title}\" status has been updated to {newStatus}.",
                            NotificationType.ServiceRequest
                    );
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing service request {RequestId} status to {NewStatus}", requestId, newStatus);
                throw;
            }
        }

        public async Task<List<ServiceRequestUpdate>> GetServiceRequestUpdatesAsync(int requestId)
        {
            try
            {
                return await _context.ServiceRequestUpdates
                    .Where(u => u.ServiceRequestId == requestId)
                    .OrderByDescending(u => u.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting updates for service request {RequestId}", requestId);
                throw;
            }
        }

        public async Task SaveServiceRequestImageAsync(int requestId, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return;
            }

            try
            {
                // Create directory for the service request images if it doesn't exist
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "service-requests", requestId.ToString());
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Save the file
                var filePath = Path.Combine(uploadPath, image.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image for service request {RequestId}", requestId);
                throw;
            }
        }
    }
} 