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
using HomeownersSubdivision.Services;

namespace HomeownersSubdivision.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public BillingController(
            ApplicationDbContext context, 
            IUserService userService,
            INotificationService notificationService)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
        }

        // GET: Billing
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Index(string status = "", string searchString = "", int page = 1)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentStatus = status;
            
            var pageSize = 10;
            var bills = _context.Bills.Include(b => b.User)
                                     .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                bills = bills.Where(b => b.Status == status);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                bills = bills.Where(b => 
                    b.Description.Contains(searchString) || 
                    b.User.FirstName.Contains(searchString) || 
                    b.User.LastName.Contains(searchString));
            }

            // Count total items for pagination
            var totalItems = await bills.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Adjust current page if needed
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated data
            var pagedBills = await bills.OrderByDescending(b => b.IssueDate)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            // Populate ViewBag with pagination info
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            // Get status options for filter
            ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue", "Cancelled" };

            return View(pagedBills);
        }

        // GET: Billing/MyBills
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> MyBills(string status = "", int page = 1)
        {
            try
            {
                var currentUser = _userService.GetCurrentUser(HttpContext);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                ViewBag.CurrentStatus = status;
                
                var pageSize = 10;
                
                // Use safer query approach
                IQueryable<Bill> bills;
                try
                {
                    bills = _context.Bills
                        .Include(b => b.Payments)
                        .Where(b => b.UserId == currentUser.Id);
                        
                    // Apply status filter
                    if (!string.IsNullOrEmpty(status))
                    {
                        bills = bills.Where(b => b.Status == status);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Database query error: " + ex.Message;
                    return View(new List<Bill>());
                }

                try
                {
                    // Get summary data
                    ViewBag.TotalBills = await bills.CountAsync();

                    // Calculate total due separately to avoid LINQ translation errors
                    var unpaidBills = await bills.Where(b => b.Status != "Paid").ToListAsync();
                    decimal totalDue = 0;
                    foreach (var bill in unpaidBills)
                    {
                        totalDue += (bill.Amount - bill.PaidAmount);
                    }
                    ViewBag.TotalDue = totalDue;

                    // Count overdue bills - avoid using the IsOverdue property directly in LINQ
                    var today = DateTime.Now;
                    ViewBag.OverdueBills = await bills.CountAsync(b => b.Status != "Paid" && b.DueDate < today);

                    // Count total items for pagination
                    var totalItems = await bills.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                    // Adjust current page if needed
                    page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                    // Get paginated data
                    var pagedBills = await bills.OrderByDescending(b => b.IssueDate)
                                            .Skip((page - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToListAsync();

                    // Populate ViewBag with pagination info
                    ViewBag.TotalPages = totalPages;
                    ViewBag.CurrentPage = page;

                    // Get status options for filter
                    ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue" };

                    // Get payment methods for quick pay with null check
                    try
                    {
                        ViewBag.PaymentMethods = await _context.PaymentMethods
                            .Where(pm => pm.UserId == currentUser.Id)
                            .OrderByDescending(pm => pm.IsDefault)
                            .ThenByDescending(pm => pm.LastUsedDate)
                            .ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        // If there's an error with payment methods, log it but continue
                        ViewBag.PaymentMethods = new List<PaymentMethod>();
                        Console.WriteLine($"Error loading payment methods: {ex.Message}");
                    }

                    return View(pagedBills);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error processing bill data: " + ex.Message;
                    
                    // Return an empty model to avoid null reference exceptions in the view
                    ViewBag.TotalBills = 0;
                    ViewBag.TotalDue = 0;
                    ViewBag.OverdueBills = 0;
                    ViewBag.TotalPages = 1;
                    ViewBag.CurrentPage = 1;
                    ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue" };
                    ViewBag.PaymentMethods = new List<PaymentMethod>();
                    
                    return View(new List<Bill>());
                }
            }
            catch (Exception ex)
            {
                // Log the exception with more details
                Console.WriteLine($"Critical error in MyBills: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "There was an error loading your bills: " + ex.Message;
                
                // Return an empty model to avoid null reference exceptions in the view
                ViewBag.TotalBills = 0;
                ViewBag.TotalDue = 0;
                ViewBag.OverdueBills = 0;
                ViewBag.TotalPages = 1;
                ViewBag.CurrentPage = 1;
                ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue" };
                ViewBag.PaymentMethods = new List<PaymentMethod>();
                
                return View(new List<Bill>());
            }
        }

        // GET: Billing/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var bill = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            // Check if the user has permission to view this bill
            if (!User.IsInRole("Administrator") && !User.IsInRole("Staff") && bill.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // Get related payment methods for quick pay if homeowner
            if (User.IsInRole("Homeowner") && bill.UserId == currentUser.Id)
            {
                ViewBag.PaymentMethods = await _context.PaymentMethods
                    .Where(pm => pm.UserId == currentUser.Id)
                    .OrderByDescending(pm => pm.IsDefault)
                    .ThenByDescending(pm => pm.LastUsedDate)
                    .ToListAsync();
            }

            return View(bill);
        }

        // GET: Billing/Create
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Create()
        {
            // Get homeowners for dropdown
            var homeowners = await _context.Users
                .Where(u => u.Role == UserRole.Homeowner)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.Homeowners = new SelectList(homeowners, "Id", "FullName");
            ViewBag.BillTypes = new List<string> { "Association Dues", "Maintenance Fee", "Special Assessment", "Fine", "Other" };

            return View();
        }

        // POST: Billing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Create([Bind("UserId,Type,Description,Amount,DueDate,Notes")] Bill bill)
        {
            if (ModelState.IsValid)
            {
                var currentUser = _userService.GetCurrentUser(HttpContext);
                
                bill.IssueDate = DateTime.Now;
                bill.Status = "Unpaid";
                
                _context.Add(bill);
                await _context.SaveChangesAsync();
                
                // Send notification to the homeowner
                await _notificationService.CreateNotificationAsync(
                    bill.UserId,
                    "New Bill Created",
                    $"A new bill has been created: {bill.Description} for ${bill.Amount}. Due date: {bill.DueDate:MM/dd/yyyy}",
                    NotificationType.Payment,
                    NotificationPriority.Normal,
                    DeliveryMethod.InApp,
                    bill.Id
                );
                
                TempData["SuccessMessage"] = "Bill created successfully.";
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            var homeowners = await _context.Users
                .Where(u => u.Role == UserRole.Homeowner)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.Homeowners = new SelectList(homeowners, "Id", "FullName", bill.UserId);
            ViewBag.BillTypes = new List<string> { "Association Dues", "Maintenance Fee", "Special Assessment", "Fine", "Other" };
            
            return View(bill);
        }

        // GET: Billing/Edit/5
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
            {
                return NotFound();
            }
            
            // Get homeowners for dropdown
            var homeowners = await _context.Users
                .Where(u => u.Role == UserRole.Homeowner)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.Homeowners = new SelectList(homeowners, "Id", "FullName", bill.UserId);
            ViewBag.BillTypes = new List<string> { "Association Dues", "Maintenance Fee", "Special Assessment", "Fine", "Other" };
            ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue", "Cancelled" };
            
            return View(bill);
        }

        // POST: Billing/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Type,Description,Amount,DueDate,Status,Notes,IssueDate")] Bill bill)
        {
            if (id != bill.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing bill to check for status change
                    var existingBill = await _context.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    bool statusChanged = existingBill.Status != bill.Status;
                    
                    _context.Update(bill);
                    await _context.SaveChangesAsync();
                    
                    // If status changed, send notification
                    if (statusChanged)
                    {
                        await _notificationService.CreateNotificationAsync(
                            bill.UserId,
                            "Bill Updated",
                            $"Your bill has been updated: {bill.Description} for ${bill.Amount}. Due date: {bill.DueDate:MM/dd/yyyy}",
                            NotificationType.Payment,
                            NotificationPriority.Normal,
                            DeliveryMethod.InApp,
                            bill.Id
                        );
                    }
                    
                    TempData["SuccessMessage"] = "Bill updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BillExists(bill.Id))
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
            
            // If we got this far, something failed, redisplay form
            var homeowners = await _context.Users
                .Where(u => u.Role == UserRole.Homeowner)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.Homeowners = new SelectList(homeowners, "Id", "FullName", bill.UserId);
            ViewBag.BillTypes = new List<string> { "Association Dues", "Maintenance Fee", "Special Assessment", "Fine", "Other" };
            ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue", "Cancelled" };
            
            return View(bill);
        }

        // GET: Billing/Delete/5
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        // POST: Billing/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bill = await _context.Bills.FindAsync(id);
            
            // Check if the bill has payments
            var hasPayments = await _context.Payments.AnyAsync(p => p.BillId == id);
            if (hasPayments)
            {
                TempData["ErrorMessage"] = "Cannot delete bill with existing payments. Please cancel the bill instead.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            if (bill != null)
            {
                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bill deleted successfully.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Billing/SendReminder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> SendReminder(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            if (bill.Status == "Paid" || bill.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Cannot send a reminder for a bill that is already paid or cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                // Calculate days overdue
                int daysOverdue = 0;
                string reminderType = "Upcoming";
                
                if (bill.DueDate < DateTime.Now)
                {
                    daysOverdue = (int)(DateTime.Now - bill.DueDate).TotalDays;
                    reminderType = "Overdue";
                }
                else if (bill.DueDate <= DateTime.Now.AddDays(7))
                {
                    reminderType = "Due Soon";
                }

                // Create message content based on reminder type
                string title = $"{reminderType} Payment Reminder";
                string message = reminderType switch
                {
                    "Overdue" => $"Your payment of ${bill.Amount:N2} for {bill.Description} is overdue by {daysOverdue} day(s). Please make your payment as soon as possible to avoid additional charges.",
                    "Due Soon" => $"Your payment of ${bill.Amount:N2} for {bill.Description} is due on {bill.DueDate:MM/dd/yyyy}. Please make your payment before the due date.",
                    _ => $"This is a reminder about your payment of ${bill.Amount:N2} for {bill.Description} due on {bill.DueDate:MM/dd/yyyy}."
                };

                // Send notification
                await _notificationService.CreateNotificationAsync(
                    bill.UserId,
                    title,
                    message,
                    NotificationType.Payment,
                    reminderType == "Overdue" ? NotificationPriority.High : NotificationPriority.Normal,
                    DeliveryMethod.All,
                    bill.Id
                );

                // Update the status to "Overdue" if it's past the due date and currently "Unpaid"
                if (bill.Status == "Unpaid" && bill.DueDate < DateTime.Now)
                {
                    bill.Status = "Overdue";
                    _context.Update(bill);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Payment reminder sent to {bill.User?.FullName}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send reminder: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Billing/GenerateReport
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> GenerateReport(DateTime? startDate = null, DateTime? endDate = null, string status = "")
        {
            // Default to current month if no dates provided
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                
            if (!endDate.HasValue)
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
                
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;
            
            var bills = _context.Bills
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Where(b => b.IssueDate >= startDate && b.IssueDate <= endDate)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(status))
            {
                bills = bills.Where(b => b.Status == status);
            }
            
            var report = await bills.OrderBy(b => b.DueDate).ToListAsync();
            
            // Calculate summary statistics
            ViewBag.TotalBills = report.Count;
            ViewBag.TotalAmount = report.Sum(b => b.Amount);
            ViewBag.TotalPaid = report.Sum(b => b.PaidAmount);
            ViewBag.TotalOutstanding = report.Sum(b => b.RemainingAmount);
            
            // Get status options for filter
            ViewBag.StatusOptions = new List<string> { "Unpaid", "Paid", "Overdue", "Cancelled" };
            
            return View(report);
        }

        // GET: Billing/CreateBulk
        [Authorize(Roles = "Administrator,Staff")]
        public IActionResult CreateBulk()
        {
            return View();
        }

        // POST: Billing/CreateBulk
        [HttpPost]
        [Authorize(Roles = "Administrator,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulk(BulkBillViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var usersByRole = await _userService.GetUsersByRoleAsync(UserRole.Homeowner);
                List<Bill> newBills = new();

                foreach (var user in usersByRole)
                {
                    var bill = new Bill
                    {
                        UserId = user.Id,
                        Amount = model.Amount,
                        DueDate = model.DueDate,
                        Description = model.Description,
                        Type = model.BillType,
                        Status = model.Status,
                        IssueDate = DateTime.Now
                    };
                    newBills.Add(bill);
                }

                await _context.Bills.AddRangeAsync(newBills);
                await _context.SaveChangesAsync();

                await _notificationService.SendBulkBillNotificationsAsync(newBills);

                TempData["SuccessMessage"] = $"Successfully created {newBills.Count} bills";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating bills: {ex.Message}");
                return View(model);
            }
        }

        // Helper method to check if bill exists
        private bool BillExists(int id)
        {
            return _context.Bills.Any(e => e.Id == id);
        }
    }
} 