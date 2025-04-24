using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace HomeownersSubdivision.Controllers
{
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public AnnouncementsController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Helper method to check if the current user is admin or staff
        private bool IsAdminOrStaff()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Administrator" || userRole == "Staff";
        }

        // GET: Announcements
        public async Task<IActionResult> Index()
        {
            try
            {
                var isAdminOrStaff = IsAdminOrStaff();
                
                // Admins and staff see all announcements
                // Regular users only see published and non-expired announcements
                var announcements = isAdminOrStaff
                    ? await _context.Announcements
                        .Include(a => a.Creator)
                        .OrderByDescending(a => a.PublishDate)
                        .ToListAsync()
                    : await _context.Announcements
                        .Include(a => a.Creator)
                        .Where(a => a.IsPublished && 
                              (a.ExpireDate == null || a.ExpireDate >= DateTime.Today))
                        .OrderByDescending(a => a.PublishDate)
                        .ToListAsync();
                
                // Pass success message from TempData if it exists
                if (TempData["SuccessMessage"] != null)
                {
                    ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
                }

                return View(announcements);
            }
            catch (Exception ex)
            {
                // Log the error and return a friendly error view
                Console.WriteLine($"Error in Announcements/Index: {ex.Message}");
                
                // You might want to create a proper error view
                ViewBag.ErrorMessage = "An error occurred while loading announcements.";
                return View("Error");
            }
        }

        // GET: Announcements/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var announcement = await _context.Announcements
                    .Include(a => a.Creator)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (announcement == null)
                {
                    return NotFound();
                }

                // Check if user is allowed to view non-published announcements
                if (!announcement.IsPublished && !IsAdminOrStaff())
                {
                    return Forbid();
                }

                return View(announcement);
            }
            catch (Exception ex)
            {
                // Log the error and return a friendly error view
                Console.WriteLine($"Error in Announcements/Details: {ex.Message}");
                
                ViewBag.ErrorMessage = "An error occurred while loading the announcement details.";
                return View("Error");
            }
        }

        // GET: Announcements/Create
        public IActionResult Create()
        {
            // Only admin and staff can create announcements
            if (!IsAdminOrStaff())
            {
                return Forbid();
            }

            // Set default publish date to today
            var announcement = new Announcement
            {
                PublishDate = DateTime.Today,
                IsPublished = true
            };
            
            return View(announcement);
        }

        // POST: Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,IsPublished,PublishDate,ExpireDate")] Announcement announcement)
        {
            try
            {
                // Only admin and staff can create announcements
                if (!IsAdminOrStaff())
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    // Safely get the creator ID from the session
                    int creatorId = 0;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    
                    if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out creatorId))
                    {
                        announcement.CreatedBy = creatorId;
                    }
                    else
                    {
                        // Default to 1 if user ID is not available (assuming admin has ID 1)
                        // You might want to handle this differently based on your application's logic
                        announcement.CreatedBy = 1;
                    }
                    
                    _context.Add(announcement);
                    await _context.SaveChangesAsync();
                    
                    // Send notifications to homeowners if the announcement is published
                    if (announcement.IsPublished)
                    {
                        // Create a notification for all homeowners
                        await _notificationService.CreateNotificationsForRoleAsync(
                            UserRole.Homeowner,
                            $"New Announcement: {announcement.Title}",
                            $"{announcement.Content.Substring(0, Math.Min(100, announcement.Content.Length))}...\n\nClick to view the full announcement.",
                            NotificationType.Announcement,
                            NotificationPriority.Normal,
                            DeliveryMethod.InApp,
                            announcement.Id
                        );
                    }
                    
                    TempData["SuccessMessage"] = "Announcement created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                return View(announcement);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in Announcements/Create: {ex.Message}");
                
                ModelState.AddModelError("", "An error occurred while creating the announcement.");
                return View(announcement);
            }
        }

        // GET: Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Only admin and staff can edit announcements
            if (!IsAdminOrStaff())
            {
                return Forbid();
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            
            return View(announcement);
        }

        // POST: Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,IsPublished,PublishDate,ExpireDate,CreatedBy")] Announcement announcement)
        {
            // Only admin and staff can edit announcements
            if (!IsAdminOrStaff())
            {
                return Forbid();
            }
            
            if (id != announcement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if announcement was previously not published but is now being published
                    var existingAnnouncement = await _context.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
                    bool wasNotPublished = existingAnnouncement != null && !existingAnnouncement.IsPublished;
                    bool isNowPublished = announcement.IsPublished;
                    
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();
                    
                    // If announcement is being published for the first time, send notification
                    if (wasNotPublished && isNowPublished)
                    {
                        await _notificationService.CreateNotificationsForRoleAsync(
                            UserRole.Homeowner,
                            $"New Announcement: {announcement.Title}",
                            $"{announcement.Content.Substring(0, Math.Min(100, announcement.Content.Length))}...\n\nClick to view the full announcement.",
                            NotificationType.Announcement,
                            NotificationPriority.Normal,
                            DeliveryMethod.InApp,
                            announcement.Id
                        );
                    }
                    // If announcement was already published but its content was updated, send update notification
                    else if (isNowPublished && existingAnnouncement != null && 
                            (existingAnnouncement.Title != announcement.Title || 
                             existingAnnouncement.Content != announcement.Content))
                    {
                        await _notificationService.CreateNotificationsForRoleAsync(
                            UserRole.Homeowner,
                            $"Updated Announcement: {announcement.Title}",
                            $"An announcement has been updated. Click to view the updated content.",
                            NotificationType.Announcement,
                            NotificationPriority.Normal,
                            DeliveryMethod.InApp,
                            announcement.Id
                        );
                    }
                    
                    TempData["SuccessMessage"] = "Announcement updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // GET: Announcements/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            // Only admin and staff can delete announcements
            if (!IsAdminOrStaff())
            {
                return Forbid();
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .Include(a => a.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcements/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Only admin and staff can delete announcements
            if (!IsAdminOrStaff())
            {
                return Forbid();
            }
            
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Announcement deleted successfully.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.Id == id);
        }
    }
} 