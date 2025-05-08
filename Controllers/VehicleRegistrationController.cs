using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Data;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class VehicleRegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleRegistrationController> _logger;

        public VehicleRegistrationController(ApplicationDbContext context, ILogger<VehicleRegistrationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: VehicleRegistration
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get the current user ID
                if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                // Find the user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Login", "Home");
                }

                // Different views for admin/staff vs homeowners
                if (user.Role == UserRole.Administrator || user.Role == UserRole.Staff)
                {
                    // Admin & Staff view - see all vehicles with management options
                    var allVehicles = await _context.VehicleRegistrations
                        .Include(v => v.Owner)
                        .OrderByDescending(v => v.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = true;
                    return View("AdminIndex", allVehicles);
                }
                else
                {
                    // Homeowner view - see only their own vehicles
                    var myVehicles = await _context.VehicleRegistrations
                        .Include(v => v.Owner)
                        .Where(v => v.OwnerId == userId)
                        .OrderByDescending(v => v.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = false;
                    return View(myVehicles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vehicle registrations");
                TempData["ErrorMessage"] = "An error occurred while retrieving vehicle registrations.";
                return View(new List<VehicleRegistration>());
            }
        }

        // GET: VehicleRegistration/Create
        public IActionResult Create()
        {
            // Pre-fill with default values
            var vehicle = new VehicleRegistration
            {
                RegistrationDate = DateTime.Now,
                IsActive = true
            };
            return View(vehicle);
        }

        // POST: VehicleRegistration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehicleRegistration vehicle)
        {
            // Remove properties that shouldn't be bound from form
            ModelState.Remove("OwnerId");
            ModelState.Remove("Owner");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("IsActive");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the current user ID
                    if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                    {
                        TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                        return RedirectToAction("Login", "Home");
                    }

                    // Set vehicle properties
                    vehicle.OwnerId = userId;
                    vehicle.RegistrationDate = DateTime.Now;
                    vehicle.CreatedAt = DateTime.Now;
                    vehicle.IsActive = true;

                    // Add to database
                    _context.Add(vehicle);
                    await _context.SaveChangesAsync();

                    // Success message
                    TempData["SuccessMessage"] = "Vehicle registered successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving vehicle registration");
                    TempData["ErrorMessage"] = "An error occurred while registering your vehicle. Please try again.";
                }
            }

            // Log validation errors
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning($"Vehicle registration form validation failed: {errors}");
            }

            // Return to form with validation errors
            return View(vehicle);
        }

        // GET: VehicleRegistration/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.VehicleRegistrations.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Security check - only owner or admin/staff can edit
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            bool isAdminOrStaff = user.Role == UserRole.Administrator || user.Role == UserRole.Staff;
            if (!isAdminOrStaff && vehicle.OwnerId != userId)
            {
                return Forbid();
            }

            return View(vehicle);
        }

        // POST: VehicleRegistration/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VehicleRegistration vehicle)
        {
            if (id != vehicle.Id)
            {
                return NotFound();
            }

            // Remove properties that shouldn't be bound from form
            ModelState.Remove("OwnerId");
            ModelState.Remove("Owner");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("RegistrationDate");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing vehicle
                    var existingVehicle = await _context.VehicleRegistrations.FindAsync(id);
                    if (existingVehicle == null)
                    {
                        return NotFound();
                    }

                    // Security check - only owner or admin/staff can edit
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    var user = await _context.Users.FindAsync(userId);

                    if (user == null)
                    {
                        return NotFound();
                    }

                    bool isAdminOrStaff = user.Role == UserRole.Administrator || user.Role == UserRole.Staff;
                    if (!isAdminOrStaff && existingVehicle.OwnerId != userId)
                    {
                        return Forbid();
                    }

                    // Update properties
                    existingVehicle.LicensePlate = vehicle.LicensePlate;
                    existingVehicle.Make = vehicle.Make;
                    existingVehicle.Model = vehicle.Model;
                    existingVehicle.Type = vehicle.Type;
                    existingVehicle.Color = vehicle.Color;
                    existingVehicle.ExpiryDate = vehicle.ExpiryDate;
                    existingVehicle.Notes = vehicle.Notes;
                    existingVehicle.IsActive = vehicle.IsActive;
                    existingVehicle.UpdatedAt = DateTime.Now;

                    // Save changes
                    await _context.SaveChangesAsync();

                    // Success message
                    TempData["SuccessMessage"] = "Vehicle updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleRegistrationExists(vehicle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating vehicle {id}");
                    TempData["ErrorMessage"] = "An error occurred while updating your vehicle. Please try again.";
                }
            }

            // Return to form with validation errors
            return View(vehicle);
        }

        // POST: VehicleRegistration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var vehicle = await _context.VehicleRegistrations.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound();
                }

                // Security check - only owner or admin/staff can delete
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
                if (!isAdminOrStaff && vehicle.OwnerId != userId)
                {
                    return Forbid();
                }

                _context.VehicleRegistrations.Remove(vehicle);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Vehicle deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vehicle {id}");
                TempData["ErrorMessage"] = "An error occurred while deleting the vehicle.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VehicleRegistration/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.VehicleRegistrations
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            // Security check - only owner or admin/staff can view details
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            if (!isAdminOrStaff && vehicle.OwnerId != userId)
            {
                return Forbid();
            }

            return View(vehicle);
        }

        // POST: VehicleRegistration/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var vehicleRegistration = await _context.VehicleRegistrations.FindAsync(id);
                if (vehicleRegistration == null)
                {
                    return NotFound();
                }

                // Toggle the IsActive status
                vehicleRegistration.IsActive = !vehicleRegistration.IsActive;
                vehicleRegistration.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                string statusMessage = vehicleRegistration.IsActive ? "activated" : "deactivated";
                _logger.LogInformation($"Vehicle registration {id} {statusMessage} by user {User.FindFirst("UserId")?.Value}");
                TempData["SuccessMessage"] = $"Vehicle registration {statusMessage} successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling status for vehicle registration {id}");
                TempData["ErrorMessage"] = "An error occurred while updating the vehicle registration status.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool VehicleRegistrationExists(int id)
        {
            return _context.VehicleRegistrations.Any(e => e.Id == id);
        }
    }
} 