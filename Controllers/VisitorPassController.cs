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
    public class VisitorPassController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisitorPassController> _logger;

        public VisitorPassController(ApplicationDbContext context, ILogger<VisitorPassController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: VisitorPass
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

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                // Different views for admin/staff vs homeowners
                if (user.Role == UserRole.Administrator || user.Role == UserRole.Staff)
                {
                    // Admin & Staff view - see all visitor passes
                    var allPasses = await _context.VisitorPasses
                        .Include(v => v.RequestedBy)
                        .Include(v => v.ApprovedBy)
                        .OrderByDescending(v => v.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = true;
                    return View("AdminIndex", allPasses);
                }
                else
                {
                    // Homeowner view - see only their own passes
                    var myPasses = await _context.VisitorPasses
                        .Where(v => v.RequestedById == userId)
                        .OrderByDescending(v => v.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = false;
                    return View(myPasses);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving visitor passes");
                TempData["ErrorMessage"] = "An error occurred while retrieving visitor passes.";
                return View(new List<VisitorPass>());
            }
        }

        // GET: VisitorPass/Create
        public IActionResult Create()
        {
            // Pre-fill with default values
            var visitorPass = new VisitorPass
            {
                VisitDate = DateTime.Now.Date,
                ExpiryDate = DateTime.Now.Date.AddDays(1),
                Status = VisitorPassStatus.Pending
            };
            return View(visitorPass);
        }

        // POST: VisitorPass/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VisitorPass visitorPass)
        {
            // Remove properties that shouldn't be bound from form
            ModelState.Remove("RequestedById");
            ModelState.Remove("RequestedBy");
            ModelState.Remove("ApprovedById");
            ModelState.Remove("ApprovedBy");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("Status");

            // Add custom validation
            if (visitorPass.VisitDate < DateTime.Now.Date)
            {
                ModelState.AddModelError("VisitDate", "Visit date must be today or a future date");
            }
            
            if (visitorPass.ExpiryDate < visitorPass.VisitDate)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry date must be after the visit date");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user
                    if (!int.TryParse(User.FindFirst("UserId")?.Value, out int userId))
                    {
                        TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                        return RedirectToAction("Login", "Home");
                    }

                    // Set required properties
                    visitorPass.RequestedById = userId;
                    visitorPass.Status = VisitorPassStatus.Pending;
                    visitorPass.CreatedAt = DateTime.Now;
                    visitorPass.UpdatedAt = DateTime.Now;

                    // Add to database
                    _context.Add(visitorPass);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Visitor pass {visitorPass.Id} created by user {userId}");
                    TempData["SuccessMessage"] = "Visitor pass requested successfully. It is awaiting approval.";
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating visitor pass");
                    TempData["ErrorMessage"] = "An error occurred while creating your visitor pass. Please try again.";
                }
            }
            else
            {
                // Log validation errors
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning($"Visitor pass validation failed: {errors}");
            }

            return View(visitorPass);
        }

        // GET: VisitorPass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var visitorPass = await _context.VisitorPasses
                .Include(v => v.RequestedBy)
                .Include(v => v.ApprovedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (visitorPass == null)
            {
                return NotFound();
            }

            // Security check - only requester or admin/staff can view details
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            if (!isAdminOrStaff && visitorPass.RequestedById != userId)
            {
                return Forbid();
            }

            return View(visitorPass);
        }

        // POST: VisitorPass/Approve/5
        [HttpPost]
        [Authorize(Roles = "Administrator,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var visitorPass = await _context.VisitorPasses.FindAsync(id);
                if (visitorPass == null)
                {
                    return NotFound();
                }
                
                // Get current user
                var approverId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                
                // Update visitor pass
                visitorPass.Status = VisitorPassStatus.Approved;
                visitorPass.ApprovedById = approverId;
                visitorPass.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Visitor pass {id} approved by user {approverId}");
                TempData["SuccessMessage"] = "Visitor pass approved successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving visitor pass {id}");
                TempData["ErrorMessage"] = "An error occurred while approving the visitor pass.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VisitorPass/Reject/5
        [HttpPost]
        [Authorize(Roles = "Administrator,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var visitorPass = await _context.VisitorPasses.FindAsync(id);
                if (visitorPass == null)
                {
                    return NotFound();
                }
                
                // Get current user
                var rejectorId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                
                // Update visitor pass
                visitorPass.Status = VisitorPassStatus.Rejected;
                visitorPass.ApprovedById = rejectorId; // Reusing for tracking who rejected
                visitorPass.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Visitor pass {id} rejected by user {rejectorId}");
                TempData["SuccessMessage"] = "Visitor pass rejected successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting visitor pass {id}");
                TempData["ErrorMessage"] = "An error occurred while rejecting the visitor pass.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VisitorPass/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var visitorPass = await _context.VisitorPasses.FindAsync(id);
                if (visitorPass == null)
                {
                    return NotFound();
                }
                
                // Security check - only requester or admin/staff can cancel
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
                if (!isAdminOrStaff && visitorPass.RequestedById != userId)
                {
                    return Forbid();
                }
                
                _context.VisitorPasses.Remove(visitorPass);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Visitor pass {id} cancelled by user {userId}");
                TempData["SuccessMessage"] = "Visitor pass cancelled successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling visitor pass {id}");
                TempData["ErrorMessage"] = "An error occurred while cancelling the visitor pass.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 