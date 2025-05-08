using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using HomeownersSubdivision.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            ApplicationDbContext context,
            IServiceRequestService serviceRequestService,
            ILogger<ServiceRequestsController> logger)
        {
            _context = context;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
        }

        // GET: ServiceRequests
        public async Task<IActionResult> Index(ServiceRequestFilterViewModel? filter = null, int page = 1)
        {
            try
            {
                // Set default filter if not provided
                filter ??= new ServiceRequestFilterViewModel();

                // Add current user ID to filter for context
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var userRole = User.FindFirst("Role")?.Value;

                // Set page size
                const int pageSize = 10;

                // Get service requests based on filter
                var serviceRequests = await _serviceRequestService.GetAllServiceRequestsAsync(filter, page, pageSize);
                var totalCount = await _serviceRequestService.GetServiceRequestsCountAsync(filter);

                // Create view models
                var serviceRequestViewModels = serviceRequests.Select(sr => new ServiceRequestViewModel
                {
                    Id = sr.Id,
                    Title = sr.Title,
                    Description = sr.Description,
                    Status = sr.Status,
                    Priority = sr.Priority,
                    RequestType = sr.RequestType,
                    SubmissionDate = sr.SubmissionDate,
                    CompletionDate = sr.CompletionDate,
                    HomeownerId = sr.HomeownerId,
                    HomeownerName = sr.Homeowner?.FirstName + " " + sr.Homeowner?.LastName ?? "Unknown",
                    AssignedToId = sr.AssignedToId,
                    AssignedToName = sr.AssignedTo?.FullName ?? "Unassigned",
                    LastUpdated = sr.LastUpdated,
                    ImageUrl = sr.ImageUrl
                }).ToList();

                // Create pagination view model
                var pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize)
                };

                // Populate status dropdown items for filter
                ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                    .Cast<ServiceRequestStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = filter.Status.HasValue && (int)filter.Status.Value == (int)s
                    })
                    .ToList();

                // Populate request type dropdown items for filter
                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = filter.Type.HasValue && (int)filter.Type.Value == (int)t
                    })
                    .ToList();

                // Populate priority dropdown items for filter
                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = filter.Priority.HasValue && (int)filter.Priority.Value == (int)p
                    })
                    .ToList();

                // Create the list view model
                var model = new ServiceRequestListViewModel
                {
                    ServiceRequests = serviceRequestViewModels,
                    Filter = filter,
                    Pagination = pagination
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service requests");
                TempData["ErrorMessage"] = "An error occurred while retrieving service requests.";
                return View(new ServiceRequestListViewModel());
            }
        }

        // GET: ServiceRequests/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                if (serviceRequest == null)
                {
                    return NotFound();
                }

                // Get updates for this service request
                var updates = await _serviceRequestService.GetServiceRequestUpdatesAsync(id);

                // Create view models
                var serviceRequestViewModel = new ServiceRequestViewModel
                {
                    Id = serviceRequest.Id,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    Status = serviceRequest.Status,
                    Priority = serviceRequest.Priority,
                    RequestType = serviceRequest.RequestType,
                    SubmissionDate = serviceRequest.SubmissionDate,
                    CompletionDate = serviceRequest.CompletionDate,
                    HomeownerId = serviceRequest.HomeownerId,
                    HomeownerName = serviceRequest.Homeowner?.FirstName + " " + serviceRequest.Homeowner?.LastName ?? "Unknown",
                    AssignedToId = serviceRequest.AssignedToId,
                    AssignedToName = serviceRequest.AssignedTo?.FullName ?? "Unassigned",
                    LastUpdated = serviceRequest.LastUpdated,
                    ImageUrl = serviceRequest.ImageUrl
                };

                var updateViewModels = updates.Select(u => new ServiceRequestUpdateViewModel
                {
                    UpdatedAt = u.UpdatedAt,
                    UpdatedByName = _context.Users.FirstOrDefault(user => user.Id == u.UpdatedById)?.FullName ?? "System",
                    Notes = u.Notes,
                    OldStatus = u.OldStatus,
                    NewStatus = u.NewStatus
                }).ToList();

                var model = new ServiceRequestDetailsViewModel
                {
                    ServiceRequest = serviceRequestViewModel,
                    Updates = updateViewModels
                };

                // Get list of staff members for assignment dropdown
                if (User.IsInRole("Administrator") || User.IsInRole("Staff"))
                {
                    ViewBag.StaffMembers = await _context.Users
                        .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Administrator)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id.ToString(),
                            Text = u.FullName,
                            Selected = u.Id == serviceRequest.AssignedToId
                        })
                        .ToListAsync();

                    ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                        .Cast<ServiceRequestStatus>()
                        .Select(s => new SelectListItem
                        {
                            Value = ((int)s).ToString(),
                            Text = s.ToString(),
                            Selected = (int)serviceRequest.Status == (int)s
                        })
                        .ToList();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing service request details for ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving service request details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ServiceRequests/Create
        public IActionResult Create()
        {
            // Set available request types and priorities
            ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                .Cast<ServiceRequestType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = t.ToString()
                })
                .ToList();

            ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                .Cast<Priority>()
                .Select(p => new SelectListItem
                {
                    Value = ((int)p).ToString(),
                    Text = p.ToString()
                })
                .ToList();

            return View(new CreateServiceRequestViewModel
            {
                Title = "",
                Description = "",
                RequestType = ServiceRequestType.Maintenance,
                Priority = Priority.Medium
            });
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceRequestViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get the current user's homeowner ID
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    var homeowner = await _context.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.Homeowner)
                        .FirstOrDefaultAsync();

                    if (homeowner == null)
                    {
                        ModelState.AddModelError("", "Unable to find your homeowner profile.");
                        return View(model);
                    }

                    // Create a new service request
                    var serviceRequest = new ServiceRequest
                    {
                        HomeownerId = homeowner.Id,
                        Title = model.Title,
                        Description = model.Description,
                        RequestType = model.RequestType,
                        Priority = model.Priority,
                        Status = ServiceRequestStatus.New,
                        SubmissionDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };

                    // Save the service request and image if provided
                    var result = await _serviceRequestService.CreateServiceRequestAsync(serviceRequest, model.Image);

                    TempData["SuccessMessage"] = "Your service request has been submitted successfully.";
                    return RedirectToAction(nameof(Details), new { id = result.Id });
                }

                // If we got this far, something failed, redisplay form
                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = (int)model.RequestType == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = (int)model.Priority == (int)p
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request");
                ModelState.AddModelError("", "An error occurred while submitting your request. Please try again.");
                
                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = (int)model.RequestType == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = (int)model.Priority == (int)p
                    })
                    .ToList();
                    
                return View(model);
            }
        }

        // GET: ServiceRequests/Edit/5
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                if (serviceRequest == null)
                {
                    return NotFound();
                }

                // Create view model
                var model = new EditServiceRequestViewModel
                {
                    Id = serviceRequest.Id,
                    Title = serviceRequest.Title,
                    Description = serviceRequest.Description,
                    Status = serviceRequest.Status,
                    Priority = serviceRequest.Priority,
                    RequestType = serviceRequest.RequestType,
                    AssignedToId = serviceRequest.AssignedToId,
                    StaffNotes = serviceRequest.StaffNotes,
                    ImageUrl = serviceRequest.ImageUrl
                };

                // Populate dropdowns
                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = (int)serviceRequest.RequestType == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = (int)serviceRequest.Priority == (int)p
                    })
                    .ToList();

                ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                    .Cast<ServiceRequestStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = (int)serviceRequest.Status == (int)s
                    })
                    .ToList();

                // Get staff members for assignment
                model.StaffMembers = await _context.Users
                    .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Administrator)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FullName,
                        Selected = u.Id == serviceRequest.AssignedToId
                    })
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing service request with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while retrieving the service request for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ServiceRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int id, EditServiceRequestViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Get the original service request
                    var serviceRequest = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                    if (serviceRequest == null)
                    {
                        return NotFound();
                    }

                    // Update service request properties
                    serviceRequest.Title = model.Title;
                    serviceRequest.Description = model.Description;
                    serviceRequest.Status = model.Status;
                    serviceRequest.Priority = model.Priority;
                    serviceRequest.RequestType = model.RequestType;
                    serviceRequest.AssignedToId = model.AssignedToId;

                    // Update the service request
                    await _serviceRequestService.UpdateServiceRequestAsync(serviceRequest, model.StaffNotes, model.NewImage);

                    TempData["SuccessMessage"] = "Service request updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = serviceRequest.Id });
                }

                // If we got this far, something failed, redisplay form
                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = (int)model.RequestType == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = (int)model.Priority == (int)p
                    })
                    .ToList();

                ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                    .Cast<ServiceRequestStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = (int)model.Status == (int)s
                    })
                    .ToList();

                // Get staff members for assignment
                model.StaffMembers = await _context.Users
                    .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.Administrator)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.FullName,
                        Selected = u.Id == model.AssignedToId
                    })
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service request with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the service request.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ServiceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _serviceRequestService.DeleteServiceRequestAsync(id);
                TempData["SuccessMessage"] = "Service request deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service request with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the service request.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ServiceRequests/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Assign(int id, int staffId)
        {
            try
            {
                var result = await _serviceRequestService.AssignServiceRequestAsync(id, staffId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Service request assigned successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign the service request.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning service request ID: {Id} to staff ID: {StaffId}", id, staffId);
                TempData["ErrorMessage"] = "An error occurred while assigning the service request.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: ServiceRequests/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ChangeStatus(int id, ServiceRequestStatus status, string notes)
        {
            try
            {
                var result = await _serviceRequestService.ChangeServiceRequestStatusAsync(id, status, notes);
                if (result)
                {
                    TempData["SuccessMessage"] = "Service request status updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update the service request status.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing service request ID: {Id} status to: {Status}", id, status);
                TempData["ErrorMessage"] = "An error occurred while updating the service request status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: ServiceRequests/MyRequests
        public async Task<IActionResult> MyRequests(int page = 1)
        {
            try
            {
                // Get current user's homeowner ID
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var homeowner = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Homeowner)
                    .FirstOrDefaultAsync();

                if (homeowner == null)
                {
                    TempData["ErrorMessage"] = "Unable to find your homeowner profile.";
                    return RedirectToAction(nameof(Index));
                }

                // Create filter for my requests
                var filter = new ServiceRequestFilterViewModel
                {
                    MyRequestsOnly = true
                };

                // Add current user ID to filter
                filter.GetType().GetProperty("CurrentUserId")?.SetValue(filter, userId);

                // Set page size
                const int pageSize = 10;

                // Get service requests based on filter
                var serviceRequests = await _serviceRequestService.GetAllServiceRequestsAsync(filter, page, pageSize);
                var totalCount = await _serviceRequestService.GetServiceRequestsCountAsync(filter);

                // Create view models
                var serviceRequestViewModels = serviceRequests.Select(sr => new ServiceRequestViewModel
                {
                    Id = sr.Id,
                    Title = sr.Title,
                    Description = sr.Description,
                    Status = sr.Status,
                    Priority = sr.Priority,
                    RequestType = sr.RequestType,
                    SubmissionDate = sr.SubmissionDate,
                    CompletionDate = sr.CompletionDate,
                    HomeownerId = sr.HomeownerId,
                    HomeownerName = sr.Homeowner?.FirstName + " " + sr.Homeowner?.LastName ?? "Unknown",
                    AssignedToId = sr.AssignedToId,
                    AssignedToName = sr.AssignedTo?.FullName ?? "Unassigned",
                    LastUpdated = sr.LastUpdated,
                    ImageUrl = sr.ImageUrl
                }).ToList();

                // Create pagination view model
                var pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize)
                };

                // Create the list view model
                var model = new ServiceRequestListViewModel
                {
                    ServiceRequests = serviceRequestViewModels,
                    Filter = filter,
                    Pagination = pagination
                };

                // Add filter dropdowns like in Index method
                ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                    .Cast<ServiceRequestStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = filter.Status.HasValue && (int)filter.Status.Value == (int)s
                    })
                    .ToList();

                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = filter.Type.HasValue && (int)filter.Type.Value == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = filter.Priority.HasValue && (int)filter.Priority.Value == (int)p
                    })
                    .ToList();

                ViewBag.MyRequestsView = true;
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's service requests");
                TempData["ErrorMessage"] = "An error occurred while retrieving your service requests.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ServiceRequests/AssignedToMe
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> AssignedToMe(int page = 1)
        {
            try
            {
                // Get current user ID
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // Create filter for assigned requests
                var filter = new ServiceRequestFilterViewModel
                {
                    MyRequestsOnly = true
                };

                // Add current user ID to filter
                filter.GetType().GetProperty("CurrentUserId")?.SetValue(filter, userId);

                // Set page size
                const int pageSize = 10;

                // Get service requests based on filter
                var serviceRequests = await _serviceRequestService.GetAllServiceRequestsAsync(filter, page, pageSize);
                var totalCount = await _serviceRequestService.GetServiceRequestsCountAsync(filter);

                // Create view models
                var serviceRequestViewModels = serviceRequests.Select(sr => new ServiceRequestViewModel
                {
                    Id = sr.Id,
                    Title = sr.Title,
                    Description = sr.Description,
                    Status = sr.Status,
                    Priority = sr.Priority,
                    RequestType = sr.RequestType,
                    SubmissionDate = sr.SubmissionDate,
                    CompletionDate = sr.CompletionDate,
                    HomeownerId = sr.HomeownerId,
                    HomeownerName = sr.Homeowner?.FirstName + " " + sr.Homeowner?.LastName ?? "Unknown",
                    AssignedToId = sr.AssignedToId,
                    AssignedToName = sr.AssignedTo?.FullName ?? "Unassigned",
                    LastUpdated = sr.LastUpdated,
                    ImageUrl = sr.ImageUrl
                }).ToList();

                // Create pagination view model
                var pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize)
                };

                // Create the list view model
                var model = new ServiceRequestListViewModel
                {
                    ServiceRequests = serviceRequestViewModels,
                    Filter = filter,
                    Pagination = pagination
                };

                // Add filter dropdowns like in Index method
                ViewBag.StatusSelectList = Enum.GetValues(typeof(ServiceRequestStatus))
                    .Cast<ServiceRequestStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = filter.Status.HasValue && (int)filter.Status.Value == (int)s
                    })
                    .ToList();

                ViewBag.RequestTypeSelectList = Enum.GetValues(typeof(ServiceRequestType))
                    .Cast<ServiceRequestType>()
                    .Select(t => new SelectListItem
                    {
                        Value = ((int)t).ToString(),
                        Text = t.ToString(),
                        Selected = filter.Type.HasValue && (int)filter.Type.Value == (int)t
                    })
                    .ToList();

                ViewBag.PrioritySelectList = Enum.GetValues(typeof(Priority))
                    .Cast<Priority>()
                    .Select(p => new SelectListItem
                    {
                        Value = ((int)p).ToString(),
                        Text = p.ToString(),
                        Selected = filter.Priority.HasValue && (int)filter.Priority.Value == (int)p
                    })
                    .ToList();

                ViewBag.AssignedToMeView = true;
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned service requests");
                TempData["ErrorMessage"] = "An error occurred while retrieving your assigned service requests.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 