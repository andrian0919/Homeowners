using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using HomeownersSubdivision.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HomeownersSubdivision.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public NotificationsController(INotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        // GET: Notifications
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            
            return View(notifications);
        }

        // GET: Notifications/Unread
        public async Task<IActionResult> Unread()
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return View("Index", notifications);
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, string returnUrl = null)
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            await _notificationService.MarkAsReadAsync(id, userId);
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(string returnUrl = null)
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            await _notificationService.MarkAllAsReadAsync(userId);
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction(nameof(Index));
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

        // AJAX: Gets the current unread count for the notification badge
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        // AJAX: Gets a few recent notifications for the notification dropdown
        [HttpGet]
        public async Task<IActionResult> GetRecentNotifications()
        {
            if (!IsUserLoggedIn(out int userId))
            {
                return PartialView("_RecentNotifications", new List<Notification>());
            }

            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId, 5);
            return PartialView("_RecentNotifications", notifications);
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
    }
} 