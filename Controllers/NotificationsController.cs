using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using HomeownersSubdivision.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            IUserService userService,
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Notifications
        public async Task<IActionResult> Index(int page = 1)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var pageSize = 20;
            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(await _context.Notifications.CountAsync(n => n.UserId == currentUser.Id) / (double)pageSize);
            
            return View(notifications);
        }

        // GET: Notifications/GetRecentNotifications
        public async Task<IActionResult> GetRecentNotifications()
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return Json(new { error = "User not found" });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            return PartialView("_NotificationsList", notifications);
        }

        // GET: Notifications/GetUnreadCount
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
        {
                return Json(new { count = 0 });
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.Id && n.Status == NotificationStatus.Unread);

            return Json(new { count });
            }

        // POST: Notifications/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            
            var success = await _notificationService.MarkAsReadAsync(id, currentUser.Id);

            return Json(new { success });
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var count = await _notificationService.MarkAllAsReadAsync(currentUser.Id);

            return Json(new { success = true, count });
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            // Get the notification
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
                
            if (notification == null)
            {
                return NotFound();
            }

            // Mark the notification as read
            await _notificationService.MarkAsReadAsync(id, userId);

            // If there's a direct link, try to redirect to it
            if (!string.IsNullOrEmpty(notification.Link))
            {
                try
                {
                    if (Url.IsLocalUrl(notification.Link))
                    {
                        return Redirect(notification.Link);
                    }
                }
                catch (Exception ex)
                {
                    // If there's an error with the link, continue to show notification details
                    TempData["ErrorMessage"] = "Could not navigate to the linked page.";
                }
            }

            // Redirect based on notification type and related item
            if (notification.Type == NotificationType.Announcement && notification.RelatedItemId.HasValue)
            {
                return RedirectToAction("Details", "Announcements", new { id = notification.RelatedItemId.Value });
            }
            else if (notification.Type == NotificationType.MaintenanceRequest && notification.RelatedItemId.HasValue)
            {
                return RedirectToAction("Details", "MaintenanceRequests", new { id = notification.RelatedItemId.Value });
            }
            else if (notification.Type == NotificationType.Event && notification.RelatedItemId.HasValue)
            {
                return RedirectToAction("Details", "Events", new { id = notification.RelatedItemId.Value });
            }

            // If no specific redirect, show the notification details
            return View(notification);
        }

        // GET: Notifications/Navigate/5
        public async Task<IActionResult> Navigate(int id)
        {
            try
            {
                var currentUser = _userService.GetCurrentUser(HttpContext);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get the notification
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);
                    
                if (notification == null)
                {
                    return NotFound();
                }

                // Mark the notification as read
                notification.Status = NotificationStatus.Read;
                notification.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // If there's a direct link, try to redirect to it
                if (!string.IsNullOrEmpty(notification.Link))
                {
                    if (Url.IsLocalUrl(notification.Link))
                    {
                        return Redirect(notification.Link);
                    }
                }

                // Handle different notification types
                switch (notification.Type)
                {
                    case NotificationType.Payment:
                        return RedirectToAction("MyBills", "Billing");
                        
                    case NotificationType.Announcement:
                        return RedirectToAction("Index", "Announcements");
                        
                    case NotificationType.MaintenanceRequest:
                    case NotificationType.ServiceRequest:
                        return RedirectToAction("Index", "ServiceRequests");
                        
                    case NotificationType.Event:
                        return RedirectToAction("Index", "Events");
                        
                    case NotificationType.Assignment:
                        return RedirectToAction("Index", "Assignments");
                        
                    case NotificationType.Update:
                        // For updates, try to use the related item ID to navigate to the appropriate page
                        if (notification.RelatedItemId.HasValue)
                        {
                            // Try to determine the type of update based on the link or related item
                            if (notification.Link?.Contains("/Billing/") == true)
                                return RedirectToAction("MyBills", "Billing");
                            else if (notification.Link?.Contains("/ServiceRequests/") == true)
                                return RedirectToAction("Index", "ServiceRequests");
                            else if (notification.Link?.Contains("/Events/") == true)
                                return RedirectToAction("Index", "Events");
                            else if (notification.Link?.Contains("/Announcements/") == true)
                                return RedirectToAction("Index", "Announcements");
                        }
                        return RedirectToAction("Index", "Home");
                        
                    case NotificationType.System:
                        // For system notifications, try to use the link if available
                        if (!string.IsNullOrEmpty(notification.Link) && Url.IsLocalUrl(notification.Link))
                        {
                            return Redirect(notification.Link);
                        }
                        return RedirectToAction("Index", "Home");
                        
                    default:
                        // Default fallback
                        if (currentUser.Role == UserRole.Homeowner)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to notification {NotificationId}", id);
                // Log the error and redirect to a safe page
                TempData["ErrorMessage"] = "There was an error processing your notification. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to check if user is logged in and get user ID
        private bool IsUserLoggedIn(out int userId)
        {
            userId = 0;
            
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out userId))
            {
                return false;
            }
            
            return true;
        }
    }
} 