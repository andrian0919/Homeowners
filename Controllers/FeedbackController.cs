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
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext context, ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Feedback
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
                    // Admin & Staff view - see all feedback
                    var allFeedback = await _context.Feedbacks
                        .Include(f => f.SubmittedBy)
                        .Include(f => f.ProcessedBy)
                        .Include(f => f.Homeowner)
                        .OrderByDescending(f => f.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = true;
                    return View("AdminIndex", allFeedback);
                }
                else
                {
                    // Homeowner view - see only their own feedback
                    var myFeedback = await _context.Feedbacks
                        .Include(f => f.ProcessedBy)
                        .Where(f => f.SubmittedById == userId)
                        .OrderByDescending(f => f.CreatedAt)
                        .ToListAsync();

                    ViewBag.IsAdminOrStaff = false;
                    return View(myFeedback);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback");
                TempData["ErrorMessage"] = "An error occurred while retrieving feedback.";
                return View(new List<Feedback>());
            }
        }

        // GET: Feedback/Create
        public IActionResult Create()
        {
            // Initialize a new feedback with default values
            var feedback = new Feedback
            {
                Type = FeedbackType.Feedback,
                Status = FeedbackStatus.Pending,
                Priority = 0, // Low priority by default
                IsPublic = false
            };
            
            return View(feedback);
        }

        // POST: Feedback/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            // Remove properties that shouldn't be bound from form
            ModelState.Remove("SubmittedBy");
            ModelState.Remove("ProcessedBy");
            ModelState.Remove("Homeowner");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    var user = await _context.Users
                        .Include(u => u.Homeowner)
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "User not found. Please log in again.";
                        return RedirectToAction("Login", "Home");
                    }

                    // Set required properties
                    feedback.SubmittedById = userId;
                    feedback.HomeownerId = user.HomeownerId;
                    feedback.Status = FeedbackStatus.Pending;
                    feedback.CreatedAt = DateTime.Now;

                    // Add to database
                    _context.Add(feedback);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Feedback {feedback.Id} created by user {userId}");
                    TempData["SuccessMessage"] = "Your feedback has been submitted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating feedback");
                    TempData["ErrorMessage"] = "An error occurred while submitting your feedback. Please try again.";
                }
            }
            else
            {
                // Log validation errors
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning($"Feedback form validation failed: {errors}");
            }

            // If we get here, something went wrong
            return View(feedback);
        }

        // GET: Feedback/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.SubmittedBy)
                .Include(f => f.ProcessedBy)
                .Include(f => f.Homeowner)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            // Security check - only the submitter or admin/staff can view details
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            bool isAdminOrStaff = user?.Role == UserRole.Administrator || user?.Role == UserRole.Staff;
            
            if (!isAdminOrStaff && feedback.SubmittedById != userId)
            {
                return Forbid();
            }

            ViewBag.IsAdminOrStaff = isAdminOrStaff;
            return View(feedback);
        }

        // GET: Feedback/Edit/5
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.SubmittedBy)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // POST: Feedback/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int id, Feedback feedback)
        {
            if (id != feedback.Id)
            {
                return NotFound();
            }

            // Remove properties that shouldn't be bound from form
            ModelState.Remove("SubmittedBy");
            ModelState.Remove("ProcessedBy");
            ModelState.Remove("Homeowner");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFeedback = await _context.Feedbacks.FindAsync(id);
                    if (existingFeedback == null)
                    {
                        return NotFound();
                    }

                    // Get current user (admin/staff)
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                    // Update properties
                    existingFeedback.Status = feedback.Status;
                    existingFeedback.Priority = feedback.Priority;
                    existingFeedback.Category = feedback.Category;
                    existingFeedback.AdminResponse = feedback.AdminResponse;
                    existingFeedback.IsPublic = feedback.IsPublic;
                    existingFeedback.UpdatedAt = DateTime.Now;
                    existingFeedback.ProcessedById = userId;

                    // If status is resolved, set the resolve date
                    if (feedback.Status == FeedbackStatus.Resolved && existingFeedback.ResolvedAt == null)
                    {
                        existingFeedback.ResolvedAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Feedback {id} updated by admin/staff {userId}");
                    TempData["SuccessMessage"] = "Feedback updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, $"Concurrency error updating feedback {id}");
                    if (!FeedbackExists(feedback.Id))
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
                    _logger.LogError(ex, $"Error updating feedback {id}");
                    TempData["ErrorMessage"] = "An error occurred while updating the feedback. Please try again.";
                }
            }

            return View(feedback);
        }

        // POST: Feedback/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Administrator,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, FeedbackStatus status, string? adminResponse = null)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null)
                {
                    return NotFound();
                }
                
                // Get current user (admin/staff)
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                
                // Update feedback status
                feedback.Status = status;
                feedback.ProcessedById = userId;
                feedback.UpdatedAt = DateTime.Now;
                
                // Set admin response if provided
                if (!string.IsNullOrEmpty(adminResponse))
                {
                    feedback.AdminResponse = adminResponse;
                }
                
                // If status is resolved, set the resolve date
                if (status == FeedbackStatus.Resolved && feedback.ResolvedAt == null)
                {
                    feedback.ResolvedAt = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Feedback {id} status updated to {status} by admin/staff {userId}");
                TempData["SuccessMessage"] = $"Feedback status updated to {status} successfully!";
                
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating feedback status {id}");
                TempData["ErrorMessage"] = "An error occurred while updating the feedback status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Feedback/Public
        public async Task<IActionResult> Public()
        {
            try
            {
                // Get all public feedback that is resolved
                var publicFeedback = await _context.Feedbacks
                    .Include(f => f.SubmittedBy)
                    .Include(f => f.ProcessedBy)
                    .Where(f => f.IsPublic && f.Status == FeedbackStatus.Resolved)
                    .OrderByDescending(f => f.ResolvedAt)
                    .ToListAsync();

                return View(publicFeedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public feedback");
                TempData["ErrorMessage"] = "An error occurred while retrieving public feedback.";
                return View(new List<Feedback>());
            }
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(f => f.Id == id);
        }
    }
} 