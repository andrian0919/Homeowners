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
    public class EmergencyContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmergencyContactController> _logger;

        public EmergencyContactController(ApplicationDbContext context, ILogger<EmergencyContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: EmergencyContact
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user
                if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                {
                    TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                var user = await _context.Users
                    .Include(u => u.Homeowner)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                // Different views for admin/staff vs homeowners
                if (user.Role == UserRole.Administrator || user.Role == UserRole.Staff)
                {
                    // Admin & Staff view - see all emergency contacts
                    var allContacts = await _context.EmergencyContacts
                        .Include(e => e.Homeowner)
                        .OrderBy(e => e.Homeowner.LastName)
                        .ThenBy(e => e.Homeowner.FirstName)
                        .ThenByDescending(e => e.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = true;
                    // Admin/Staff don't need a homeowner profile to view all contacts
                    return View("AdminIndex", allContacts);
                }
                else if (user.Homeowner != null)
                {
                    // Homeowner view - see only their own emergency contacts
                    var myContacts = await _context.EmergencyContacts
                        .Where(e => e.HomeownerId == user.Homeowner.Id)
                        .OrderByDescending(e => e.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = false;
                    ViewBag.HomeownerId = user.Homeowner.Id;
                    return View(myContacts);
                }
                else
                {
                    // The user does not have an associated homeowner record
                    _logger.LogWarning($"User {userId} attempted to access emergency contacts but has no homeowner record");
                    TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                    return View("NoHomeowner");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency contacts");
                TempData["ErrorMessage"] = "An error occurred while retrieving emergency contacts.";
                return View(new List<EmergencyContact>());
            }
        }

        // GET: EmergencyContact/Create
        public async Task<IActionResult> Create()
        {
            // Check if user has a homeowner record first
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
            {
                TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Users
                .Include(u => u.Homeowner)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Admins and staff can create emergency contacts, even without a homeowner profile
            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            
            if (user?.Homeowner == null && !isAdminOrStaff)
            {
                TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                return RedirectToAction(nameof(Index));
            }

            var contact = new EmergencyContact
            {
                IsActive = true
            };
            
            // For admins/staff, we'll let them select a homeowner in the view
            ViewBag.IsAdminOrStaff = isAdminOrStaff;
            if (isAdminOrStaff)
            {
                ViewBag.Homeowners = await _context.Homeowners
                    .OrderBy(h => h.LastName)
                    .ThenBy(h => h.FirstName)
                    .ToListAsync();
            }
            
            return View(contact);
        }

        // POST: EmergencyContact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmergencyContact contact)
        {
            // Remove properties that shouldn't be bound from form
            ModelState.Remove("Homeowner");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("IsActive");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    var user = await _context.Users
                        .Include(u => u.Homeowner)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;

                    // Only set HomeownerId from user's profile if it's not already set (from form) and user is not admin/staff
                    if (contact.HomeownerId == 0 && !isAdminOrStaff)
                    {
                        if (user?.Homeowner == null)
                        {
                            _logger.LogWarning($"User {userId} attempted to create emergency contact but has no homeowner record");
                            TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        contact.HomeownerId = user.Homeowner.Id;
                    }
                    
                    // If HomeownerId is still 0 and user is admin/staff, that's an error
                    if (contact.HomeownerId == 0 && isAdminOrStaff)
                    {
                        ModelState.AddModelError("HomeownerId", "Please select a homeowner for this emergency contact.");
                        ViewBag.IsAdminOrStaff = true;
                        ViewBag.Homeowners = await _context.Homeowners
                            .OrderBy(h => h.LastName)
                            .ThenBy(h => h.FirstName)
                            .ToListAsync();
                        return View(contact);
                    }

                    // Set other required properties
                    contact.CreatedAt = DateTime.Now;
                    contact.IsActive = true;

                    // Add to database
                    _context.Add(contact);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Emergency contact {contact.Id} created for homeowner {contact.HomeownerId}");
                    TempData["SuccessMessage"] = "Emergency contact added successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating emergency contact");
                    TempData["ErrorMessage"] = "An error occurred while adding the emergency contact. Please try again.";
                }
            }
            else
            {
                // Log validation errors
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning($"Emergency contact form validation failed: {errors}");
            }

            // If we get here, something went wrong
            int currentUserId = 0;
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
            {
                int.TryParse(userIdClaim.Value, out currentUserId);
            }
            
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
                
            bool currentUserIsAdminOrStaff = currentUser?.Role == UserRole.Administrator || currentUser?.Role == UserRole.Staff;
            
            ViewBag.IsAdminOrStaff = currentUserIsAdminOrStaff;
            if (currentUserIsAdminOrStaff)
            {
                ViewBag.Homeowners = await _context.Homeowners
                    .OrderBy(h => h.LastName)
                    .ThenBy(h => h.FirstName)
                    .ToListAsync();
            }
            
            return View(contact);
        }

        // GET: EmergencyContact/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.EmergencyContacts
                .Include(e => e.Homeowner)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Security check - only the homeowner or admin/staff can edit
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Homeowner)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            
            // Admin/Staff can always edit, homeowners can only edit their own
            if (!isAdminOrStaff)
            {
                if (user?.Homeowner == null)
                {
                    TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                    return RedirectToAction(nameof(Index));
                }
                
                if (contact.HomeownerId != user.Homeowner.Id)
                {
                    return Forbid();
                }
            }

            return View(contact);
        }

        // POST: EmergencyContact/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmergencyContact contact)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            // Remove properties that shouldn't be bound from form
            ModelState.Remove("Homeowner");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                try
                {
                    // Security check - only the homeowner or admin/staff can edit
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    var user = await _context.Users
                        .Include(u => u.Homeowner)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    var existingContact = await _context.EmergencyContacts.FindAsync(id);
                    if (existingContact == null)
                    {
                        return NotFound();
                    }

                    bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
                    
                    // Admin/Staff can always edit, homeowners can only edit their own
                    if (!isAdminOrStaff)
                    {
                        if (user?.Homeowner == null)
                        {
                            TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                            return RedirectToAction(nameof(Index));
                        }
                        
                        if (existingContact.HomeownerId != user.Homeowner.Id)
                        {
                            return Forbid();
                        }
                    }

                    // Update properties
                    existingContact.Name = contact.Name;
                    existingContact.PhoneNumber = contact.PhoneNumber;
                    existingContact.Email = contact.Email;
                    existingContact.Type = contact.Type;
                    existingContact.Relationship = contact.Relationship;
                    existingContact.Address = contact.Address;
                    existingContact.Notes = contact.Notes;
                    existingContact.IsActive = contact.IsActive;
                    existingContact.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Emergency contact updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, $"Concurrency error updating emergency contact {id}");
                    if (!EmergencyContactExists(contact.Id))
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
                    _logger.LogError(ex, $"Error updating emergency contact {id}");
                    TempData["ErrorMessage"] = "An error occurred while updating the emergency contact. Please try again.";
                }
            }

            return View(contact);
        }

        // POST: EmergencyContact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var contact = await _context.EmergencyContacts.FindAsync(id);
                if (contact == null)
                {
                    return NotFound();
                }

                // Security check - only the homeowner or admin/staff can delete
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Homeowner)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
                
                // Admin/Staff can always delete, homeowners can only delete their own
                if (!isAdminOrStaff)
                {
                    if (user?.Homeowner == null)
                    {
                        TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    if (contact.HomeownerId != user.Homeowner.Id)
                    {
                        return Forbid();
                    }
                }

                _context.EmergencyContacts.Remove(contact);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Emergency contact deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting emergency contact {id}");
                TempData["ErrorMessage"] = "An error occurred while deleting the emergency contact.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EmergencyContact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.EmergencyContacts
                .Include(e => e.Homeowner)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Security check - only the homeowner or admin/staff can view details
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Homeowner)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            
            // Admin/Staff can always view details, homeowners can only view their own
            if (!isAdminOrStaff)
            {
                if (user?.Homeowner == null)
                {
                    TempData["ErrorMessage"] = "Your homeowner profile is not set up. Please contact administration.";
                    return RedirectToAction(nameof(Index));
                }
                
                if (contact.HomeownerId != user.Homeowner.Id)
                {
                    return Forbid();
                }
            }

            return View(contact);
        }

        private bool EmergencyContactExists(int id)
        {
            return _context.EmergencyContacts.Any(e => e.Id == id);
        }
    }
} 