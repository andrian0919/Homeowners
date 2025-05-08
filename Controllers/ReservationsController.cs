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
using ClosedXML.Excel;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;

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
                _logger.LogError(ex, "Error in Reservations/Index: {Message}", ex.Message);
                
                // Add error message to TempData
                TempData["ErrorMessage"] = "An error occurred while loading reservations. Please try again.";
                
                // Return a simpler error view with minimal dependencies
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "An error occurred while loading reservations."
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
                            Link = $"/Reservations/Details/{reservation.Id}",
                            IsSent = true,
                            SentAt = DateTime.Now
                        };
                        
                        _context.Notifications.Add(notification);
                    }
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception notificationEx)
                    {
                        // Log but don't fail if notification creation fails
                        _logger.LogError(notificationEx, "Error creating notifications for reservation {ReservationId}", reservation.Id);
                    }

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
                                    Link = $"/Reservations/Details/{reservation.Id}",
                                    IsSent = true,
                                    SentAt = DateTime.Now
                                };
                                
                                _context.Notifications.Add(notification);
                                
                                try
                                {
                                    await _context.SaveChangesAsync();
                                }
                                catch (Exception notificationEx)
                                {
                                    // Log but don't fail if notification creation fails
                                    _logger.LogError(notificationEx, "Error creating status change notification for reservation {ReservationId}", reservation.Id);
                                }
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
                            Link = $"/Reservations/Details/{reservation.Id}",
                            IsSent = true,
                            SentAt = DateTime.Now
                        };
                        
                        _context.Notifications.Add(notification);
                    }
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception notificationEx)
                    {
                        // Log but don't fail if notification creation fails
                        _logger.LogError(notificationEx, "Error creating admin notifications for cancelled reservation {ReservationId}", reservation.Id);
                    }
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
                            Link = $"/Reservations/Details/{reservation.Id}",
                            IsSent = true,
                            SentAt = DateTime.Now
                        };
                        
                        _context.Notifications.Add(notification);
                        
                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception notificationEx)
                        {
                            // Log but don't fail if notification creation fails
                            _logger.LogError(notificationEx, "Error creating user notification for cancelled reservation {ReservationId}", reservation.Id);
                        }
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
                openingTime = facility.OpeningTime.Hours.ToString("00") + ":" + facility.OpeningTime.Minutes.ToString("00"),
                closingTime = facility.ClosingTime.Hours.ToString("00") + ":" + facility.ClosingTime.Minutes.ToString("00")
            });
        }

        // GET: Reservations/AdminIndex
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> AdminIndex(string searchString, int? facilityId, string status, string dateRange, int page = 1)
        {
            try
            {
                int pageSize = 10;
                
                // Start with all reservations
                var reservationsQuery = _context.FacilityReservations
                    .Include(r => r.Facility)
                    .Include(r => r.User)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    reservationsQuery = reservationsQuery.Where(r => 
                        r.User.FirstName.Contains(searchString) ||
                        r.User.LastName.Contains(searchString) ||
                        r.Facility.Name.Contains(searchString) ||
                        r.Purpose.Contains(searchString));
                }

                if (facilityId.HasValue)
                {
                    reservationsQuery = reservationsQuery.Where(r => r.FacilityId == facilityId.Value);
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, out var statusEnum))
                {
                    reservationsQuery = reservationsQuery.Where(r => r.Status == statusEnum);
                }

                // Date range filters
                var today = DateTime.Today;
                switch (dateRange)
                {
                    case "Today":
                        reservationsQuery = reservationsQuery.Where(r => r.ReservationDate == today);
                        break;
                    case "ThisWeek":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(6);
                        reservationsQuery = reservationsQuery.Where(r => r.ReservationDate >= startOfWeek && r.ReservationDate <= endOfWeek);
                        break;
                    case "ThisMonth":
                        var startOfMonth = new DateTime(today.Year, today.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                        reservationsQuery = reservationsQuery.Where(r => r.ReservationDate >= startOfMonth && r.ReservationDate <= endOfMonth);
                        break;
                    case "NextMonth":
                        var startOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
                        var endOfNextMonth = startOfNextMonth.AddMonths(1).AddDays(-1);
                        reservationsQuery = reservationsQuery.Where(r => r.ReservationDate >= startOfNextMonth && r.ReservationDate <= endOfNextMonth);
                        break;
                    case "Past":
                        reservationsQuery = reservationsQuery.Where(r => r.ReservationDate < today);
                        break;
                }

                // Order by date (descending) and then by start time
                reservationsQuery = reservationsQuery.OrderByDescending(r => r.ReservationDate).ThenBy(r => r.StartTime);

                // Calculate total items and pages
                var totalItems = await reservationsQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                
                // Adjust current page if needed
                page = Math.Min(Math.Max(1, page), Math.Max(1, totalPages));

                // Get the reservations for the current page
                var reservations = await reservationsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get all facilities for the dropdown
                var facilities = await _context.Facilities
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                // Calculate summary statistics
                var totalReservations = await _context.FacilityReservations.CountAsync();
                var pendingReservations = await _context.FacilityReservations.CountAsync(r => r.Status == ReservationStatus.Pending);
                var todayReservations = await _context.FacilityReservations.CountAsync(r => r.ReservationDate == today);
                var totalRevenue = await _context.FacilityReservations
                    .Where(r => r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.Rejected)
                    .SumAsync(r => r.TotalCost);

                // Set up ViewBag properties for the view
                ViewBag.CurrentFilter = searchString;
                ViewBag.CurrentFacility = facilityId?.ToString();
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentDateRange = dateRange;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Facilities = facilities;
                ViewBag.TotalReservations = totalReservations;
                ViewBag.PendingReservations = pendingReservations;
                ViewBag.TodayReservations = todayReservations;
                ViewBag.TotalRevenue = totalRevenue;

                return View(reservations);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error in Reservations/AdminIndex: {Message}", ex.Message);
                
                // Add error message to TempData
                TempData["ErrorMessage"] = "An error occurred while loading the admin reservations. Please try again.";
                
                // Return a simpler error view with minimal dependencies
                return View("Error", new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "An error occurred while loading the admin reservations."
                });
            }
        }

        // POST: Reservations/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Approve(int id)
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

                // Only allow approval if the reservation is pending
                if (reservation.Status != ReservationStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending reservations can be approved.";
                    return RedirectToAction(nameof(AdminIndex));
                }

                // Update reservation status
                reservation.Status = ReservationStatus.Approved;
                reservation.UpdatedAt = DateTime.Now;
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                // Create a notification for the user
                if (reservation.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = reservation.User.Id,
                        Title = "Reservation Approved",
                        Message = $"Your reservation for {reservation.Facility.Name} on {reservation.ReservationDate:MM/dd/yyyy} from {reservation.StartTime:hh\\:mm} to {reservation.EndTime:hh\\:mm} has been approved.",
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread,
                        Type = NotificationType.System,
                        Priority = NotificationPriority.Normal,
                        DeliveryMethod = DeliveryMethod.InApp,
                        Link = $"/Reservations/Details/{reservation.Id}",
                        IsSent = true,
                        SentAt = DateTime.Now
                    };
                    
                    _context.Notifications.Add(notification);
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception notificationEx)
                    {
                        // Log but don't fail if notification creation fails
                        _logger.LogError(notificationEx, "Error creating approval notification for reservation {ReservationId}", reservation.Id);
                    }
                }

                TempData["SuccessMessage"] = $"Reservation #{reservation.Id} has been approved. Notification sent to the user.";
                return RedirectToAction(nameof(AdminIndex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving reservation: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while approving the reservation.";
                return RedirectToAction(nameof(AdminIndex));
            }
        }

        // POST: Reservations/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
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

                // Only allow rejection if the reservation is pending
                if (reservation.Status != ReservationStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending reservations can be rejected.";
                    return RedirectToAction(nameof(AdminIndex));
                }

                // Update reservation status and rejection reason
                reservation.Status = ReservationStatus.Rejected;
                reservation.RejectionReason = rejectionReason;
                reservation.UpdatedAt = DateTime.Now;
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                // Create a notification for the user
                if (reservation.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = reservation.User.Id,
                        Title = "Reservation Rejected",
                        Message = $"Your reservation for {reservation.Facility.Name} on {reservation.ReservationDate:MM/dd/yyyy} has been rejected." + 
                                  (!string.IsNullOrEmpty(rejectionReason) ? $" Reason: {rejectionReason}" : ""),
                        CreatedAt = DateTime.Now,
                        Status = NotificationStatus.Unread,
                        Type = NotificationType.System,
                        Priority = NotificationPriority.High,
                        DeliveryMethod = DeliveryMethod.InApp,
                        Link = $"/Reservations/Details/{reservation.Id}",
                        IsSent = true,
                        SentAt = DateTime.Now
                    };
                    
                    _context.Notifications.Add(notification);
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception notificationEx)
                    {
                        // Log but don't fail if notification creation fails
                        _logger.LogError(notificationEx, "Error creating rejection notification for reservation {ReservationId}", reservation.Id);
                    }
                }

                TempData["SuccessMessage"] = $"Reservation #{reservation.Id} has been rejected." + 
                                            (!string.IsNullOrEmpty(rejectionReason) ? " Notification sent to the user with the rejection reason." : "");
                return RedirectToAction(nameof(AdminIndex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting reservation: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while rejecting the reservation.";
                return RedirectToAction(nameof(AdminIndex));
            }
        }

        // POST: Reservations/MarkComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> MarkComplete(int id)
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

                // Only allow marking as complete if the reservation is approved
                if (reservation.Status != ReservationStatus.Approved)
                {
                    TempData["ErrorMessage"] = "Only approved reservations can be marked as complete.";
                    return RedirectToAction(nameof(AdminIndex));
                }

                // Update reservation status
                reservation.Status = ReservationStatus.Completed;
                reservation.UpdatedAt = DateTime.Now;
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Reservation #{reservation.Id} has been marked as complete.";
                return RedirectToAction(nameof(AdminIndex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking reservation as complete: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while marking the reservation as complete.";
                return RedirectToAction(nameof(AdminIndex));
            }
        }

        // GET: Reservations/ExportToExcel
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                var reservations = await _context.FacilityReservations
                    .Include(r => r.Facility)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.ReservationDate)
                    .ThenBy(r => r.StartTime)
                    .ToListAsync();

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Reservations");
                    
                    // Add header row
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "User";
                    worksheet.Cell(1, 3).Value = "Facility";
                    worksheet.Cell(1, 4).Value = "Date";
                    worksheet.Cell(1, 5).Value = "Start Time";
                    worksheet.Cell(1, 6).Value = "End Time";
                    worksheet.Cell(1, 7).Value = "Guests";
                    worksheet.Cell(1, 8).Value = "Status";
                    worksheet.Cell(1, 9).Value = "Cost";
                    worksheet.Cell(1, 10).Value = "Created At";
                    
                    // Style header row
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
                    
                    // Add data rows
                    for (int i = 0; i < reservations.Count; i++)
                    {
                        var reservation = reservations[i];
                        var rowIndex = i + 2;
                        
                        worksheet.Cell(rowIndex, 1).Value = reservation.Id;
                        worksheet.Cell(rowIndex, 2).Value = $"{reservation.User.FirstName} {reservation.User.LastName}";
                        worksheet.Cell(rowIndex, 3).Value = reservation.Facility.Name;
                        worksheet.Cell(rowIndex, 4).Value = reservation.ReservationDate.ToShortDateString();
                        worksheet.Cell(rowIndex, 5).Value = $"{reservation.StartTime.Hours:00}:{reservation.StartTime.Minutes:00}";
                        worksheet.Cell(rowIndex, 6).Value = $"{reservation.EndTime.Hours:00}:{reservation.EndTime.Minutes:00}";
                        worksheet.Cell(rowIndex, 7).Value = reservation.NumberOfGuests;
                        worksheet.Cell(rowIndex, 8).Value = reservation.Status.ToString();
                        worksheet.Cell(rowIndex, 9).Value = reservation.TotalCost;
                        worksheet.Cell(rowIndex, 10).Value = reservation.CreatedAt.ToString();
                        
                        // Apply conditional formatting based on status
                        if (reservation.Status == ReservationStatus.Approved)
                        {
                            worksheet.Cell(rowIndex, 8).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                        }
                        else if (reservation.Status == ReservationStatus.Cancelled)
                        {
                            worksheet.Cell(rowIndex, 8).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCoral;
                        }
                        else if (reservation.Status == ReservationStatus.Pending)
                        {
                            worksheet.Cell(rowIndex, 8).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                        }
                    }
                    
                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();
                    
                    // Generate a unique filename with timestamp
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var fileName = $"Reservations_{timestamp}.xlsx";
                    
                    // Convert to a byte array
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reservations to Excel: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while exporting reservations to Excel.";
                return RedirectToAction(nameof(AdminIndex));
            }
        }

        // GET: Reservations/GenerateReport
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> GenerateReport()
        {
            try
            {
                // Get reservation data
                var reservations = await _context.FacilityReservations
                    .Include(r => r.Facility)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.ReservationDate)
                    .ThenBy(r => r.StartTime)
                    .ToListAsync();
                
                // Calculate summary statistics
                var totalReservations = reservations.Count;
                var pendingReservations = reservations.Count(r => r.Status == ReservationStatus.Pending);
                var approvedReservations = reservations.Count(r => r.Status == ReservationStatus.Approved);
                var cancelledReservations = reservations.Count(r => r.Status == ReservationStatus.Cancelled);
                var completedReservations = reservations.Count(r => r.Status == ReservationStatus.Completed);
                var totalRevenue = reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.Rejected)
                    .Sum(r => r.TotalCost);
                
                // Group reservations by facility
                var facilityStats = reservations
                    .GroupBy(r => r.Facility.Name)
                    .Select(g => new 
                    {
                        FacilityName = g.Key,
                        Count = g.Count(),
                        Revenue = g.Where(r => r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.Rejected)
                                  .Sum(r => r.TotalCost)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();
                
                // Create a PDF document
                using (var memoryStream = new MemoryStream())
                {
                    using (var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 50, 50, 50, 50))
                    {
                        var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, memoryStream);
                        document.Open();
                        
                        // Add title
                        var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 18);
                        var title = new iTextSharp.text.Paragraph($"Facility Reservations Report", titleFont);
                        title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                        title.SpacingAfter = 20;
                        document.Add(title);
                        
                        // Add generation info
                        var normalFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10);
                        var boldFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 10);
                        
                        document.Add(new iTextSharp.text.Paragraph($"Generated on: {DateTime.Now}", normalFont));
                        document.Add(new iTextSharp.text.Paragraph(" "));  // Space
                        
                        // Add summary statistics
                        var summaryTitle = new iTextSharp.text.Paragraph("Summary Statistics", boldFont);
                        summaryTitle.SpacingAfter = 10;
                        document.Add(summaryTitle);
                        
                        // Create a summary table
                        var summaryTable = new iTextSharp.text.pdf.PdfPTable(2);
                        summaryTable.WidthPercentage = 50;
                        summaryTable.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
                        summaryTable.SpacingAfter = 20;
                        
                        // Add summary rows
                        AddRow(summaryTable, "Total Reservations:", totalReservations.ToString(), boldFont, normalFont);
                        AddRow(summaryTable, "Pending:", pendingReservations.ToString(), boldFont, normalFont);
                        AddRow(summaryTable, "Approved:", approvedReservations.ToString(), boldFont, normalFont);
                        AddRow(summaryTable, "Cancelled:", cancelledReservations.ToString(), boldFont, normalFont);
                        AddRow(summaryTable, "Completed:", completedReservations.ToString(), boldFont, normalFont);
                        AddRow(summaryTable, "Total Revenue:", string.Format("₱{0:N2}", totalRevenue), boldFont, normalFont);
                        
                        document.Add(summaryTable);
                        
                        // Add facility statistics
                        var facilityTitle = new iTextSharp.text.Paragraph("Facility Statistics", boldFont);
                        facilityTitle.SpacingAfter = 10;
                        document.Add(facilityTitle);
                        
                        // Create a facility table
                        var facilityTable = new iTextSharp.text.pdf.PdfPTable(3);
                        facilityTable.WidthPercentage = 100;
                        facilityTable.SpacingAfter = 20;
                        
                        // Add facility table headers
                        var facilityHeaders = new string[] { "Facility", "Reservations", "Revenue" };
                        foreach (var header in facilityHeaders)
                        {
                            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(header, boldFont));
                            cell.BackgroundColor = new iTextSharp.text.BaseColor(220, 220, 220);
                            cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            facilityTable.AddCell(cell);
                        }
                        
                        // Add facility rows
                        foreach (var stat in facilityStats)
                        {
                            facilityTable.AddCell(new iTextSharp.text.Phrase(stat.FacilityName, normalFont));
                            
                            var countCell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(stat.Count.ToString(), normalFont));
                            countCell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                            facilityTable.AddCell(countCell);
                            
                            var revenueCell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(string.Format("₱{0:N2}", stat.Revenue), normalFont));
                            revenueCell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
                            facilityTable.AddCell(revenueCell);
                        }
                        
                        document.Add(facilityTable);
                        
                        // Add recent reservations table
                        var recentTitle = new iTextSharp.text.Paragraph("Recent Reservations", boldFont);
                        recentTitle.SpacingAfter = 10;
                        document.Add(recentTitle);
                        
                        // Create a reservations table
                        var recentTable = new iTextSharp.text.pdf.PdfPTable(6);
                        recentTable.WidthPercentage = 100;
                        
                        // Add column widths
                        float[] columnWidths = new float[] { 1.5f, 2.5f, 2f, 1.5f, 1.5f, 1.5f };
                        recentTable.SetWidths(columnWidths);
                        
                        // Add reservation table headers
                        var reservationHeaders = new string[] { "ID", "User", "Facility", "Date", "Status", "Cost" };
                        foreach (var header in reservationHeaders)
                        {
                            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(header, boldFont));
                            cell.BackgroundColor = new iTextSharp.text.BaseColor(220, 220, 220);
                            cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            recentTable.AddCell(cell);
                        }
                        
                        // Add reservation rows (just the most recent 20)
                        foreach (var reservation in reservations.Take(20))
                        {
                            recentTable.AddCell(new iTextSharp.text.Phrase(reservation.Id.ToString(), normalFont));
                            recentTable.AddCell(new iTextSharp.text.Phrase($"{reservation.User.FirstName} {reservation.User.LastName}", normalFont));
                            recentTable.AddCell(new iTextSharp.text.Phrase(reservation.Facility.Name, normalFont));
                            recentTable.AddCell(new iTextSharp.text.Phrase(reservation.ReservationDate.ToShortDateString(), normalFont));
                            
                            // Status cell with color
                            var statusCell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(reservation.Status.ToString(), normalFont));
                            statusCell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                            
                            // Add background color based on status
                            switch (reservation.Status)
                            {
                                case ReservationStatus.Pending:
                                    statusCell.BackgroundColor = new iTextSharp.text.BaseColor(255, 255, 200);
                                    break;
                                case ReservationStatus.Approved:
                                    statusCell.BackgroundColor = new iTextSharp.text.BaseColor(200, 255, 200);
                                    break;
                                case ReservationStatus.Cancelled:
                                    statusCell.BackgroundColor = new iTextSharp.text.BaseColor(255, 200, 200);
                                    break;
                                case ReservationStatus.Completed:
                                    statusCell.BackgroundColor = new iTextSharp.text.BaseColor(200, 200, 255);
                                    break;
                            }
                            
                            recentTable.AddCell(statusCell);
                            
                            var costCell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(string.Format("₱{0:N2}", reservation.TotalCost), normalFont));
                            costCell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_RIGHT;
                            recentTable.AddCell(costCell);
                        }
                        
                        document.Add(recentTable);
                        
                        document.Close();
                    }
                    
                    // Return the PDF
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var fileName = $"ReservationReport_{timestamp}.pdf";
                    var content = memoryStream.ToArray();
                    
                    return File(content, "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reservation report: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while generating the reservation report.";
                return RedirectToAction(nameof(AdminIndex));
            }
        }
        
        // Helper method to add a row to a PDF table
        private void AddRow(iTextSharp.text.pdf.PdfPTable table, string col1, string col2, 
                          iTextSharp.text.Font font1, iTextSharp.text.Font font2)
        {
            var cell1 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(col1, font1));
            cell1.Border = iTextSharp.text.Rectangle.NO_BORDER;
            table.AddCell(cell1);
            
            var cell2 = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(col2, font2));
            cell2.Border = iTextSharp.text.Rectangle.NO_BORDER;
            table.AddCell(cell2);
        }
    }
} 