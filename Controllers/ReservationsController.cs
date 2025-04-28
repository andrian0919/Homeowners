using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(ApplicationDbContext context, ILogger<ReservationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user
                var username = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                List<FacilityReservation> reservations;
                
                // Administrators and staff can see all reservations
                if (User.IsInRole("Administrator") || User.IsInRole("Staff"))
                {
                    reservations = await _context.FacilityReservations
                        .Include(r => r.Facility)
                        .Include(r => r.User)
                        .OrderByDescending(r => r.ReservationDate)
                        .ThenBy(r => r.StartTime)
                        .ToListAsync();
                }
                else
                {
                    // Regular users can only see their own reservations
                    reservations = await _context.FacilityReservations
                        .Include(r => r.Facility)
                        .Where(r => r.UserId == user.Id)
                        .OrderByDescending(r => r.ReservationDate)
                        .ThenBy(r => r.StartTime)
                        .ToListAsync();
                }

                var facilities = await _context.Facilities
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                var viewModel = new FacilityReservationListViewModel
                {
                    Reservations = reservations,
                    AvailableFacilities = facilities
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error in Reservations/Index");
                
                // Add error message to TempData
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                
                // Return to error view with details
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.FacilityReservations
                .Include(r => r.Facility)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the reservation or an admin/staff
            var username = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (reservation.UserId != currentUser.Id && 
                !User.IsInRole("Administrator") && 
                !User.IsInRole("Staff"))
            {
                return Forbid();
            }

            var viewModel = new ReservationDetailsViewModel
            {
                Reservation = reservation,
                User = reservation.User,
                Facility = reservation.Facility
            };

            return View(viewModel);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create(int? facilityId)
        {
            var facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            if (!facilities.Any())
            {
                TempData["ErrorMessage"] = "No facilities are available for reservation.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CreateReservationViewModel
            {
                AvailableFacilities = facilities,
                ReservationDate = DateTime.Today.AddDays(1)
            };

            // If a facility ID was provided, set it as the selected facility
            if (facilityId.HasValue)
            {
                var facility = facilities.FirstOrDefault(f => f.Id == facilityId);
                if (facility != null)
                {
                    viewModel.FacilityId = facility.Id;
                    viewModel.FacilityName = facility.Name;
                    viewModel.HourlyRate = facility.HourlyRate;
                    
                    // Set default times based on facility opening hours
                    viewModel.StartTime = facility.OpeningTime;
                    viewModel.EndTime = facility.OpeningTime.Add(new TimeSpan(1, 0, 0)); // Default 1 hour reservation
                }
            }
            else if (facilities.Any())
            {
                // Set default facility to the first one
                var facility = facilities.First();
                viewModel.FacilityId = facility.Id;
                viewModel.FacilityName = facility.Name;
                viewModel.HourlyRate = facility.HourlyRate;
                
                // Set default times based on facility opening hours
                viewModel.StartTime = facility.OpeningTime;
                viewModel.EndTime = facility.OpeningTime.Add(new TimeSpan(1, 0, 0)); // Default 1 hour reservation
            }

            return View(viewModel);
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FacilityId,ReservationDate,StartTime,EndTime,NumberOfGuests,Purpose")] CreateReservationViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get the current user
                    var username = User.Identity?.Name;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                    
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    // Get the selected facility
                    var facility = await _context.Facilities.FindAsync(viewModel.FacilityId);
                    if (facility == null)
                    {
                        ModelState.AddModelError("FacilityId", "Selected facility does not exist.");
                        await PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Validate reservation time
                    if (viewModel.StartTime >= viewModel.EndTime)
                    {
                        ModelState.AddModelError("EndTime", "End time must be after start time.");
                        await PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Validate against facility opening hours
                    if (viewModel.StartTime < facility.OpeningTime)
                    {
                        ModelState.AddModelError("StartTime", $"Start time must be after facility opening time ({facility.OpeningTime:hh\\:mm}).");
                        await PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    if (viewModel.EndTime > facility.ClosingTime)
                    {
                        ModelState.AddModelError("EndTime", $"End time must be before facility closing time ({facility.ClosingTime:hh\\:mm}).");
                        await PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Check for overlapping reservations
                    bool hasOverlap = await _context.FacilityReservations
                        .Where(r => r.FacilityId == viewModel.FacilityId &&
                               r.ReservationDate == viewModel.ReservationDate &&
                               r.Status != ReservationStatus.Rejected &&
                               r.Status != ReservationStatus.Cancelled &&
                               ((r.StartTime <= viewModel.StartTime && viewModel.StartTime < r.EndTime) ||
                               (r.StartTime < viewModel.EndTime && viewModel.EndTime <= r.EndTime) ||
                               (viewModel.StartTime <= r.StartTime && r.StartTime < viewModel.EndTime)))
                        .AnyAsync();

                    if (hasOverlap)
                    {
                        ModelState.AddModelError("", "There is already a reservation for this facility at the selected time.");
                        await PrepareCreateViewModel(viewModel);
                        return View(viewModel);
                    }

                    // Calculate total cost
                    var duration = viewModel.EndTime - viewModel.StartTime;
                    decimal totalCost = facility.HourlyRate * (decimal)duration.TotalHours;

                    // Create the reservation
                    var reservation = new FacilityReservation
                    {
                        FacilityId = viewModel.FacilityId,
                        UserId = user.Id,
                        ReservationDate = viewModel.ReservationDate,
                        StartTime = viewModel.StartTime,
                        EndTime = viewModel.EndTime,
                        NumberOfGuests = viewModel.NumberOfGuests,
                        Purpose = viewModel.Purpose,
                        Status = ReservationStatus.Pending,
                        CreatedAt = DateTime.Now,
                        TotalCost = totalCost
                    };

                    _context.Add(reservation);
                    await _context.SaveChangesAsync();
                    
                    // Create a notification for the admin/staff
                    var admins = await _context.Users
                        .Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Staff)
                        .ToListAsync();
                    
                    string userFullName = $"{user.FirstName} {user.LastName}";
                    
                    foreach (var admin in admins)
                    {
                        var notification = new Notification
                        {
                            UserId = admin.Id,
                            Title = "New Facility Reservation",
                            Message = $"A new reservation has been made for {facility.Name} by {userFullName} on {viewModel.ReservationDate:MM/dd/yyyy} from {viewModel.StartTime:hh\\:mm} to {viewModel.EndTime:hh\\:mm}.",
                            CreatedAt = DateTime.Now,
                            Status = NotificationStatus.Unread,
                            Type = NotificationType.System,
                            Priority = NotificationPriority.Normal,
                            DeliveryMethod = DeliveryMethod.InApp,
                            Link = $"/Reservations/Details/{reservation.Id}"
                        };
                        
                        _context.Notifications.Add(notification);
                    }
                    
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                await PrepareCreateViewModel(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                TempData["ErrorMessage"] = "An error occurred while creating the reservation. Please try again.";
                await PrepareCreateViewModel(viewModel);
                return View(viewModel);
            }
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.FacilityReservations
                .Include(r => r.Facility)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the reservation or an admin/staff
            var username = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            bool isAdminOrStaff = User.IsInRole("Administrator") || User.IsInRole("Staff");
            
            // Only allow editing if:
            // 1. The user owns the reservation and it's still pending or
            // 2. The user is an admin/staff
            if ((reservation.UserId != currentUser.Id || reservation.Status != ReservationStatus.Pending) && 
                !isAdminOrStaff)
            {
                return Forbid();
            }

            var facilities = await _context.Facilities
                .Where(f => f.IsActive || f.Id == reservation.FacilityId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var viewModel = new EditReservationViewModel
            {
                Id = reservation.Id,
                FacilityId = reservation.FacilityId,
                ReservationDate = reservation.ReservationDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                NumberOfGuests = reservation.NumberOfGuests,
                Purpose = reservation.Purpose,
                Status = reservation.Status,
                RejectionReason = reservation.RejectionReason,
                AvailableFacilities = facilities,
                FacilityName = reservation.Facility.Name,
                HourlyRate = reservation.Facility.HourlyRate,
                TotalCost = reservation.TotalCost
            };

            // Only admins can change status
            ViewBag.CanChangeStatus = isAdminOrStaff;
            
            return View(viewModel);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FacilityId,ReservationDate,StartTime,EndTime,NumberOfGuests,Purpose,Status,RejectionReason")] EditReservationViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return NotFound();
                }

                var reservation = await _context.FacilityReservations
                    .Include(r => r.Facility)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (reservation == null)
                {
                    return NotFound();
                }

                // Check if the current user is the owner of the reservation or an admin/staff
                var username = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                bool isAdminOrStaff = User.IsInRole("Administrator") || User.IsInRole("Staff");
                
                // Only allow editing if:
                // 1. The user owns the reservation and it's still pending or
                // 2. The user is an admin/staff
                if ((reservation.UserId != currentUser.Id || reservation.Status != ReservationStatus.Pending) && 
                    !isAdminOrStaff)
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    var facility = await _context.Facilities.FindAsync(viewModel.FacilityId);
                    if (facility == null)
                    {
                        ModelState.AddModelError("FacilityId", "Selected facility does not exist.");
                        await PrepareEditViewModel(viewModel);
                        ViewBag.CanChangeStatus = isAdminOrStaff;
                        return View(viewModel);
                    }

                    // Regular users can only update details, not status
                    if (!isAdminOrStaff)
                    {
                        viewModel.Status = reservation.Status;
                    }
                    else if (viewModel.Status == ReservationStatus.Rejected && string.IsNullOrWhiteSpace(viewModel.RejectionReason))
                    {
                        ModelState.AddModelError("RejectionReason", "Please provide a reason for rejection.");
                        await PrepareEditViewModel(viewModel);
                        ViewBag.CanChangeStatus = isAdminOrStaff;
                        return View(viewModel);
                    }

                    // Only validate time constraints if the reservation isn't being cancelled or rejected
                    if (viewModel.Status != ReservationStatus.Cancelled && viewModel.Status != ReservationStatus.Rejected)
                    {
                        // Validate reservation time
                        if (viewModel.StartTime >= viewModel.EndTime)
                        {
                            ModelState.AddModelError("EndTime", "End time must be after start time.");
                            await PrepareEditViewModel(viewModel);
                            ViewBag.CanChangeStatus = isAdminOrStaff;
                            return View(viewModel);
                        }

                        // Validate against facility opening hours
                        if (viewModel.StartTime < facility.OpeningTime)
                        {
                            ModelState.AddModelError("StartTime", $"Start time must be after facility opening time ({facility.OpeningTime:hh\\:mm}).");
                            await PrepareEditViewModel(viewModel);
                            ViewBag.CanChangeStatus = isAdminOrStaff;
                            return View(viewModel);
                        }

                        if (viewModel.EndTime > facility.ClosingTime)
                        {
                            ModelState.AddModelError("EndTime", $"End time must be before facility closing time ({facility.ClosingTime:hh\\:mm}).");
                            await PrepareEditViewModel(viewModel);
                            ViewBag.CanChangeStatus = isAdminOrStaff;
                            return View(viewModel);
                        }

                        // Check for overlapping reservations (excluding the current one)
                        bool hasOverlap = await _context.FacilityReservations
                            .Where(r => r.Id != id &&
                                   r.FacilityId == viewModel.FacilityId &&
                                   r.ReservationDate == viewModel.ReservationDate &&
                                   r.Status != ReservationStatus.Rejected &&
                                   r.Status != ReservationStatus.Cancelled &&
                                   ((r.StartTime <= viewModel.StartTime && viewModel.StartTime < r.EndTime) ||
                                   (r.StartTime < viewModel.EndTime && viewModel.EndTime <= r.EndTime) ||
                                   (viewModel.StartTime <= r.StartTime && r.StartTime < viewModel.EndTime)))
                            .AnyAsync();

                        if (hasOverlap)
                        {
                            ModelState.AddModelError("", "There is already a reservation for this facility at the selected time.");
                            await PrepareEditViewModel(viewModel);
                            ViewBag.CanChangeStatus = isAdminOrStaff;
                            return View(viewModel);
                        }
                    }

                    try
                    {
                        // Calculate total cost
                        var duration = viewModel.EndTime - viewModel.StartTime;
                        decimal totalCost = facility.HourlyRate * (decimal)duration.TotalHours;

                        // Update reservation
                        reservation.FacilityId = viewModel.FacilityId;
                        reservation.ReservationDate = viewModel.ReservationDate;
                        reservation.StartTime = viewModel.StartTime;
                        reservation.EndTime = viewModel.EndTime;
                        reservation.NumberOfGuests = viewModel.NumberOfGuests;
                        reservation.Purpose = viewModel.Purpose;
                        reservation.Status = viewModel.Status;
                        reservation.RejectionReason = viewModel.RejectionReason;
                        reservation.UpdatedAt = DateTime.Now;
                        reservation.TotalCost = totalCost;

                        _context.Update(reservation);
                        await _context.SaveChangesAsync();

                        // Create a notification for the reservation owner if status changed
                        if (isAdminOrStaff && reservation.UserId != currentUser.Id)
                        {
                            var user = await _context.Users.FindAsync(reservation.UserId);
                            if (user != null)
                            {
                                var statusText = viewModel.Status.ToString();
                                var message = $"Your reservation for {facility.Name} on {viewModel.ReservationDate:MM/dd/yyyy} has been {statusText.ToLower()}.";
                                if (viewModel.Status == ReservationStatus.Rejected && !string.IsNullOrEmpty(viewModel.RejectionReason))
                                {
                                    message += $" Reason: {viewModel.RejectionReason}";
                                }

                                var notification = new Notification
                                {
                                    UserId = user.Id,
                                    Title = $"Reservation {statusText}",
                                    Message = message,
                                    CreatedAt = DateTime.Now,
                                    Status = NotificationStatus.Unread,
                                    Type = NotificationType.System,
                                    Priority = NotificationPriority.Normal,
                                    DeliveryMethod = DeliveryMethod.InApp,
                                    Link = $"/Reservations/Details/{reservation.Id}"
                                };
                                
                                _context.Notifications.Add(notification);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex, "Concurrency exception when updating reservation {ReservationId}", id);
                        if (!ReservationExists(viewModel.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "This reservation was modified by another user. Please try again.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                await PrepareEditViewModel(viewModel);
                ViewBag.CanChangeStatus = isAdminOrStaff;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing reservation {ReservationId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the reservation. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Reservations/Cancel/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.FacilityReservations
                .Include(r => r.Facility)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the reservation or an admin/staff
            var username = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (reservation.UserId != currentUser.Id && 
                !User.IsInRole("Administrator") && 
                !User.IsInRole("Staff"))
            {
                return Forbid();
            }

            // Only allow cancellation if the reservation is pending or approved
            if (reservation.Status != ReservationStatus.Pending && 
                reservation.Status != ReservationStatus.Approved)
            {
                TempData["ErrorMessage"] = "This reservation cannot be cancelled.";
                return RedirectToAction(nameof(Details), new { id = reservation.Id });
            }

            return View(reservation);
        }

        // POST: Reservations/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            try
            {
                var reservation = await _context.FacilityReservations
                    .Include(r => r.Facility)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (reservation == null)
                {
                    return NotFound();
                }

                // Check if the current user is the owner of the reservation or an admin/staff
                var username = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                bool isAdminOrStaff = User.IsInRole("Administrator") || User.IsInRole("Staff");

                if (reservation.UserId != currentUser.Id && !isAdminOrStaff)
                {
                    return Forbid();
                }

                // Only allow cancellation if the reservation is pending or approved
                if (reservation.Status != ReservationStatus.Pending && 
                    reservation.Status != ReservationStatus.Approved)
                {
                    TempData["ErrorMessage"] = "This reservation cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id = reservation.Id });
                }

                // Update reservation status
                reservation.Status = ReservationStatus.Cancelled;
                reservation.UpdatedAt = DateTime.Now;
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                // Create a notification for the admin if a user is cancelling
                if (!isAdminOrStaff)
                {
                    var admins = await _context.Users
                        .Where(u => u.Role == UserRole.Administrator || u.Role == UserRole.Staff)
                        .ToListAsync();
                    
                    string userFullName = $"{currentUser.FirstName} {currentUser.LastName}";
                    
                    foreach (var admin in admins)
                    {
                        var notification = new Notification
                        {
                            UserId = admin.Id,
                            Title = "Reservation Cancelled",
                            Message = $"A reservation for {reservation.Facility.Name} on {reservation.ReservationDate:MM/dd/yyyy} has been cancelled by {userFullName}.",
                            CreatedAt = DateTime.Now,
                            Status = NotificationStatus.Unread,
                            Type = NotificationType.System,
                            Priority = NotificationPriority.Normal,
                            DeliveryMethod = DeliveryMethod.InApp,
                            Link = $"/Reservations/Details/{reservation.Id}"
                        };
                        
                        _context.Notifications.Add(notification);
                    }
                    
                    await _context.SaveChangesAsync();
                }
                // Create a notification for the user if an admin is cancelling
                else if (reservation.UserId != currentUser.Id)
                {
                    var user = await _context.Users.FindAsync(reservation.UserId);
                    if (user != null)
                    {
                        var notification = new Notification
                        {
                            UserId = user.Id,
                            Title = "Reservation Cancelled",
                            Message = $"Your reservation for {reservation.Facility.Name} on {reservation.ReservationDate:MM/dd/yyyy} has been cancelled by an administrator.",
                            CreatedAt = DateTime.Now,
                            Status = NotificationStatus.Unread,
                            Type = NotificationType.System,
                            Priority = NotificationPriority.Normal,
                            DeliveryMethod = DeliveryMethod.InApp,
                            Link = $"/Reservations/Details/{reservation.Id}"
                        };
                        
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation {ReservationId}", id);
                TempData["ErrorMessage"] = "An error occurred while cancelling the reservation. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to prepare the create view model
        private async Task PrepareCreateViewModel(CreateReservationViewModel viewModel)
        {
            var facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();

            viewModel.AvailableFacilities = facilities;

            var facility = facilities.FirstOrDefault(f => f.Id == viewModel.FacilityId);
            if (facility != null)
            {
                viewModel.FacilityName = facility.Name;
                viewModel.HourlyRate = facility.HourlyRate;
                
                // Calculate total cost
                var duration = viewModel.EndTime - viewModel.StartTime;
                viewModel.TotalCost = facility.HourlyRate * (decimal)duration.TotalHours;
            }
        }

        // Helper method to prepare the edit view model
        private async Task PrepareEditViewModel(EditReservationViewModel viewModel)
        {
            var facilities = await _context.Facilities
                .Where(f => f.IsActive || f.Id == viewModel.FacilityId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            viewModel.AvailableFacilities = facilities;

            var facility = facilities.FirstOrDefault(f => f.Id == viewModel.FacilityId);
            if (facility != null)
            {
                viewModel.FacilityName = facility.Name;
                viewModel.HourlyRate = facility.HourlyRate;
                
                // Calculate total cost
                var duration = viewModel.EndTime - viewModel.StartTime;
                viewModel.TotalCost = facility.HourlyRate * (decimal)duration.TotalHours;
            }
        }

        private bool ReservationExists(int id)
        {
            return _context.FacilityReservations.Any(e => e.Id == id);
        }

        // API endpoint to check facility availability
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int facilityId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeReservationId = null)
        {
            if (startTime >= endTime)
            {
                return Json(new { available = false, message = "End time must be after start time." });
            }

            var facility = await _context.Facilities.FindAsync(facilityId);
            if (facility == null)
            {
                return Json(new { available = false, message = "Facility not found." });
            }

            // Validate against facility opening hours
            if (startTime < facility.OpeningTime)
            {
                return Json(new { available = false, message = $"Start time must be after facility opening time ({facility.OpeningTime:hh\\:mm})." });
            }

            if (endTime > facility.ClosingTime)
            {
                return Json(new { available = false, message = $"End time must be before facility closing time ({facility.ClosingTime:hh\\:mm})." });
            }

            // Check for overlapping reservations
            var query = _context.FacilityReservations
                .Where(r => r.FacilityId == facilityId &&
                       r.ReservationDate == date &&
                       r.Status != ReservationStatus.Rejected &&
                       r.Status != ReservationStatus.Cancelled &&
                       ((r.StartTime <= startTime && startTime < r.EndTime) ||
                       (r.StartTime < endTime && endTime <= r.EndTime) ||
                       (startTime <= r.StartTime && r.StartTime < endTime)));

            // Exclude the current reservation if editing
            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }

            bool hasOverlap = await query.AnyAsync();

            if (hasOverlap)
            {
                return Json(new { available = false, message = "There is already a reservation for this facility at the selected time." });
            }

            // Calculate total cost
            var duration = endTime - startTime;
            decimal totalCost = facility.HourlyRate * (decimal)duration.TotalHours;

            return Json(new { 
                available = true, 
                message = "The facility is available at the selected time.",
                hourlyRate = facility.HourlyRate,
                totalCost = totalCost,
                formattedTotalCost = string.Format("{0:C}", totalCost)
            });
        }

        // Get facility info
        [HttpGet]
        public async Task<IActionResult> GetFacilityInfo(int facilityId)
        {
            var facility = await _context.Facilities.FindAsync(facilityId);
            if (facility == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = facility.Id,
                name = facility.Name,
                type = facility.Type.ToString(),
                maxCapacity = facility.MaxCapacity,
                hourlyRate = facility.HourlyRate,
                formattedHourlyRate = string.Format("{0:C}", facility.HourlyRate),
                openingTime = facility.OpeningTime.ToString(@"hh\:mm"),
                closingTime = facility.ClosingTime.ToString(@"hh\:mm")
            });
        }
    }
} 