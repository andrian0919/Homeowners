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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public PaymentsController(
            ApplicationDbContext context, 
            IUserService userService,
            INotificationService notificationService)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
        }

        // GET: Payments - for staff and admin
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> Index(int? homeownerId = null, string status = "", string dateRange = "", int page = 1)
        {
            ViewBag.SelectedHomeownerId = homeownerId;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDateRange = dateRange;
            
            var pageSize = 10;
            var payments = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .AsQueryable();

            // Apply homeowner filter
            if (homeownerId.HasValue)
            {
                payments = payments.Where(p => p.UserId == homeownerId);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status);
            }

            // Apply date range filter
            if (!string.IsNullOrEmpty(dateRange))
            {
                var today = DateTime.Today;
                switch (dateRange)
                {
                    case "today":
                        payments = payments.Where(p => p.PaymentDate.Date == today);
                        break;
                    case "week":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        payments = payments.Where(p => p.PaymentDate >= startOfWeek);
                        break;
                    case "month":
                        var startOfMonth = new DateTime(today.Year, today.Month, 1);
                        payments = payments.Where(p => p.PaymentDate >= startOfMonth);
                        break;
                    case "year":
                        var startOfYear = new DateTime(today.Year, 1, 1);
                        payments = payments.Where(p => p.PaymentDate >= startOfYear);
                        break;
                }
            }

            // Count total items for pagination
            var totalItems = await payments.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Adjust current page if needed
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated data
            var pagedPayments = await payments.OrderByDescending(p => p.PaymentDate)
                                           .Skip((page - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToListAsync();

            // Populate pagination info
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            // Get homeowners for filter dropdown
            var homeowners = await _context.Users
                .Where(u => u.Role == UserRole.Homeowner)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            return View(pagedPayments);
        }

        // GET: Payments/MyPayments - for homeowners
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> MyPayments(string status = "", string dateRange = "", int page = 1)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.SelectedStatus = status;
            ViewBag.SelectedDateRange = dateRange;
            
            var pageSize = 10;
            var payments = _context.Payments
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .Where(p => p.UserId == currentUser.Id)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status);
            }

            // Apply date range filter
            if (!string.IsNullOrEmpty(dateRange))
            {
                var today = DateTime.Today;
                switch (dateRange)
                {
                    case "month":
                        var oneMonthAgo = today.AddMonths(-1);
                        payments = payments.Where(p => p.PaymentDate >= oneMonthAgo);
                        break;
                    case "quarter":
                        var threeMonthsAgo = today.AddMonths(-3);
                        payments = payments.Where(p => p.PaymentDate >= threeMonthsAgo);
                        break;
                    case "year":
                        var startOfYear = new DateTime(today.Year, 1, 1);
                        payments = payments.Where(p => p.PaymentDate >= startOfYear);
                        break;
                }
            }

            // Get summary data
            ViewBag.TotalPaid = await payments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount);
            
            // Get outstanding amount from bills
            var outstandingAmount = await _context.Bills
                .Where(b => b.UserId == currentUser.Id && b.Status != "Paid")
                .SumAsync(b => b.RemainingAmount);
            ViewBag.TotalOutstanding = outstandingAmount;
            
            // Get last payment date
            var lastPayment = await payments
                .Where(p => p.Status == "Completed")
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefaultAsync();
            ViewBag.LastPaymentDate = lastPayment?.PaymentDate;
            
            // Get default payment method
            var defaultPaymentMethod = await _context.PaymentMethods
                .Where(pm => pm.UserId == currentUser.Id && pm.IsDefault)
                .FirstOrDefaultAsync();
            ViewBag.DefaultPaymentMethod = defaultPaymentMethod?.DisplayName ?? "None";

            // Get upcoming bills
            ViewBag.UpcomingBills = await _context.Bills
                .Where(b => b.UserId == currentUser.Id && 
                           b.Status != "Paid" &&
                           b.DueDate >= DateTime.Today)
                .OrderBy(b => b.DueDate)
                .Take(5)
                .ToListAsync();

            // Count total items for pagination
            var totalItems = await payments.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Adjust current page if needed
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated data
            var pagedPayments = await payments.OrderByDescending(p => p.PaymentDate)
                                           .Skip((page - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToListAsync();

            // Populate pagination info
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(pagedPayments);
        }

        // GET: Payments/PaymentHistory
        [Authorize(Roles = "Homeowner,Administrator,Staff")]
        public async Task<IActionResult> PaymentHistory(string status = "", DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Default date range to last 6 months if not specified
            if (!fromDate.HasValue)
            {
                fromDate = DateTime.Today.AddMonths(-6);
            }
            
            if (!toDate.HasValue)
            {
                toDate = DateTime.Today.AddDays(1); // Include today
            }

            var pageSize = 10;
            var payments = _context.Payments
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .AsQueryable();

            // If homeowner, only show their payments
            if (currentUser.Role == UserRole.Homeowner)
            {
                payments = payments.Where(p => p.UserId == currentUser.Id);
            }

            // Apply date range filter
            payments = payments.Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate);

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status);
            }

            // Get summary data
            ViewBag.TotalPayments = await payments.CountAsync();
            ViewBag.TotalAmountPaid = await payments
                .Where(p => p.Status == "Completed" && !p.IsRefunded)
                .SumAsync(p => p.Amount);
            
            // Get payments this month
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ViewBag.PaymentsThisMonth = await payments
                .Where(p => p.PaymentDate >= startOfMonth)
                .CountAsync();

            // Count total items for pagination
            var totalItems = await payments.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Adjust current page if needed
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get paginated data
            var pagedPayments = await payments
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate ViewBag with filter values for form
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            
            // Pagination info
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(pagedPayments);
        }

        // GET: Payments/Details/5
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

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to view this payment
            if (!User.IsInRole("Administrator") && !User.IsInRole("Staff") && payment.UserId != currentUser.Id)
            {
                return Forbid();
            }

            return View(payment);
        }

        // GET: Payments/MakePayment
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> MakePayment(int? billId = null)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Create a new payment model
            var payment = new Payment
            {
                UserId = currentUser.Id,
                PaymentDate = DateTime.Now,
                Status = "Pending"
            };

            // If billId is provided, pre-fill bill information
            if (billId.HasValue)
            {
                var bill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.Id == billId && b.UserId == currentUser.Id);
                    
                if (bill != null)
                {
                    payment.BillId = bill.Id;
                    payment.Amount = bill.RemainingAmount;
                    ViewBag.Bill = bill;
                }
            }

            // Get user's saved payment methods
            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(pm => pm.UserId == currentUser.Id)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.LastUsedDate)
                .ToListAsync();

            // Get bills that need payment
            // Fix: Cannot use RemainingAmount in EF Core query directly as it's a computed property
            var userBills = await _context.Bills
                .Where(b => b.UserId == currentUser.Id && b.Status != "Paid")
                .Include(b => b.Payments)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
                
            // Filter in memory after loading from database
            ViewBag.UnpaidBills = userBills
                .Where(b => b.RemainingAmount > 0)
                .ToList();

            return View(payment);
        }

        // POST: Payments/MakePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> MakePayment([Bind("BillId,Amount,PaymentMethodId,Notes")] Payment payment)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                payment.UserId = currentUser.Id;
                payment.PaymentDate = DateTime.Now;
                payment.Status = "Pending";
                payment.TransactionId = GenerateTransactionId();

                // Handle the payment processing
                try
                {
                    // Simulate payment processing delay
                    await Task.Delay(1000);

                    // In a real system, this would connect to a payment gateway
                    // For now, we'll just mark it as completed
                    payment.Status = "Completed";
                    payment.ProcessedDate = DateTime.Now;

                    _context.Add(payment);
                    await _context.SaveChangesAsync();

                    // Update payment method last used date if used
                    if (payment.PaymentMethodId.HasValue)
                    {
                        var paymentMethod = await _context.PaymentMethods
                            .FindAsync(payment.PaymentMethodId.Value);
                        if (paymentMethod != null)
                        {
                            paymentMethod.LastUsedDate = DateTime.Now;
                            _context.Update(paymentMethod);
                        }
                    }

                    // Update bill status if bill is fully paid
                    if (payment.BillId.HasValue)
                    {
                        await UpdateBillStatus(payment.BillId.Value);
                    }

                    await _context.SaveChangesAsync();

                    // Send notification
                    await _notificationService.CreateNotificationAsync(
                        currentUser.Id,
                        "Payment Successful",
                        $"Your payment of ${payment.Amount} has been processed successfully.",
                        NotificationType.Payment,
                        NotificationPriority.Normal,
                        DeliveryMethod.InApp,
                        payment.Id
                    );

                    TempData["SuccessMessage"] = "Payment processed successfully!";
                    return RedirectToAction(nameof(Details), new { id = payment.Id });
                }
                catch (Exception ex)
                {
                    // In case of error, mark payment as failed
                    payment.Status = "Failed";
                    payment.Notes = $"Payment processing error: {ex.Message}";
                    _context.Add(payment);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", "Payment processing failed. Please try again later.");
                }
            }

            // If we got this far, something failed, redisplay form
            // Get user's saved payment methods
            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(pm => pm.UserId == currentUser.Id)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.LastUsedDate)
                .ToListAsync();

            // Get bills that need payment
            // Fix: Cannot use RemainingAmount in EF Core query directly as it's a computed property
            var userBills = await _context.Bills
                .Where(b => b.UserId == currentUser.Id && b.Status != "Paid")
                .Include(b => b.Payments)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
                
            // Filter in memory after loading from database
            ViewBag.UnpaidBills = userBills
                .Where(b => b.RemainingAmount > 0)
                .ToList();

            // Get bill details if billId is provided
            if (payment.BillId.HasValue)
            {
                ViewBag.Bill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.Id == payment.BillId);
            }

            return View(payment);
        }

        // GET: Payments/PaymentMethods
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> PaymentMethods()
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var paymentMethods = await _context.PaymentMethods
                .Where(pm => pm.UserId == currentUser.Id)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.LastUsedDate)
                .ToListAsync();

            return View(paymentMethods);
        }

        // GET: Payments/AddPaymentMethod
        [Authorize(Roles = "Homeowner")]
        public IActionResult AddPaymentMethod()
        {
            return View();
        }

        // POST: Payments/AddPaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> AddPaymentMethod([Bind("Name,Type,LastFourDigits,CardType,ExpirationDate,BankName,AccountNumber,RoutingNumber,IsDefault")] PaymentMethod paymentMethod)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                paymentMethod.UserId = currentUser.Id;
                paymentMethod.CreatedDate = DateTime.Now;

                // If this is set as default, remove default flag from other methods
                if (paymentMethod.IsDefault)
                {
                    var existingDefaults = await _context.PaymentMethods
                        .Where(pm => pm.UserId == currentUser.Id && pm.IsDefault)
                        .ToListAsync();

                    foreach (var method in existingDefaults)
                    {
                        method.IsDefault = false;
                        _context.Update(method);
                    }
                }

                _context.Add(paymentMethod);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment method added successfully.";
                return RedirectToAction(nameof(PaymentMethods));
            }
            return View(paymentMethod);
        }

        // GET: Payments/EditPaymentMethod/5
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> EditPaymentMethod(int? id)
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

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);

            if (paymentMethod == null)
            {
                return NotFound();
            }

            return View(paymentMethod);
        }

        // POST: Payments/EditPaymentMethod/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> EditPaymentMethod(int id, [Bind("Id,Name,Type,LastFourDigits,CardType,ExpirationDate,BankName,AccountNumber,RoutingNumber,IsDefault,CreatedDate")] PaymentMethod paymentMethod)
        {
            if (id != paymentMethod.Id)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check that this payment method belongs to the current user
            var existingMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == id);
                
            if (existingMethod == null || existingMethod.UserId != currentUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    paymentMethod.UserId = currentUser.Id;

                    // If this is set as default, remove default flag from other methods
                    if (paymentMethod.IsDefault && !existingMethod.IsDefault)
                    {
                        var existingDefaults = await _context.PaymentMethods
                            .Where(pm => pm.UserId == currentUser.Id && pm.IsDefault && pm.Id != id)
                            .ToListAsync();

                        foreach (var method in existingDefaults)
                        {
                            method.IsDefault = false;
                            _context.Update(method);
                        }
                    }

                    _context.Update(paymentMethod);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Payment method updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentMethodExists(paymentMethod.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(PaymentMethods));
            }
            return View(paymentMethod);
        }

        // GET: Payments/DeletePaymentMethod/5
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> DeletePaymentMethod(int? id)
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

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);
                
            if (paymentMethod == null)
            {
                return NotFound();
            }

            return View(paymentMethod);
        }

        // POST: Payments/DeletePaymentMethod/5
        [HttpPost, ActionName("DeletePaymentMethod")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> DeletePaymentMethodConfirmed(int id)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);
                
            if (paymentMethod == null)
            {
                return NotFound();
            }

            // Check if there are recent payments using this method
            var recentPayments = await _context.Payments
                .AnyAsync(p => p.PaymentMethodId == id && 
                           p.PaymentDate > DateTime.Now.AddDays(-30) &&
                           p.Status == "Completed");
                           
            if (recentPayments)
            {
                TempData["ErrorMessage"] = "Cannot delete payment method used for recent payments.";
                return RedirectToAction(nameof(DeletePaymentMethod), new { id });
            }

            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Payment method deleted successfully.";
            return RedirectToAction(nameof(PaymentMethods));
        }

        // POST: Payments/SetDefaultPaymentMethod/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);
                
            if (paymentMethod == null)
            {
                return NotFound();
            }

            // Clear default flag on all other methods
            var currentDefaults = await _context.PaymentMethods
                .Where(pm => pm.UserId == currentUser.Id && pm.IsDefault)
                .ToListAsync();

            foreach (var method in currentDefaults)
            {
                method.IsDefault = false;
                _context.Update(method);
            }

            // Set this method as default
            paymentMethod.IsDefault = true;
            _context.Update(paymentMethod);
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Default payment method updated.";
            return RedirectToAction(nameof(PaymentMethods));
        }

        // POST: Payments/ProcessPayment/5 - for admin/staff to manually process a payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ProcessPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Bill)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Pending");
                
            if (payment == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser(HttpContext);

            payment.Status = "Completed";
            payment.ProcessedDate = DateTime.Now;
            payment.ProcessedBy = $"{currentUser?.FirstName} {currentUser?.LastName}";
            
            _context.Update(payment);

            // Update bill status if applicable
            if (payment.BillId.HasValue)
            {
                await UpdateBillStatus(payment.BillId.Value);
            }

            await _context.SaveChangesAsync();

            // Send notification to user
            await _notificationService.CreateNotificationAsync(
                payment.UserId,
                "Payment Completed",
                $"Your payment of ${payment.Amount} has been processed and marked as completed.",
                NotificationType.Payment,
                NotificationPriority.Normal,
                DeliveryMethod.InApp,
                payment.Id
            );
            
            TempData["SuccessMessage"] = "Payment processed successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Payments/CancelPayment/5 - for admin/staff to mark a payment as failed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> CancelPayment(int id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Pending");
                
            if (payment == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser(HttpContext);

            payment.Status = "Failed";
            payment.ProcessedDate = DateTime.Now;
            payment.ProcessedBy = $"{currentUser?.FirstName} {currentUser?.LastName}";
            
            _context.Update(payment);
            await _context.SaveChangesAsync();

            // Send notification to user
            await _notificationService.CreateNotificationAsync(
                payment.UserId,
                "Payment Failed",
                $"Your payment of ${payment.Amount} has been marked as failed. Please contact the administration for assistance.",
                NotificationType.Payment,
                NotificationPriority.Normal,
                DeliveryMethod.InApp,
                payment.Id
            );
            
            TempData["SuccessMessage"] = "Payment marked as failed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Payments/RefundPayment/5 - for admin/staff to refund a completed payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> RefundPayment(int id, string refundReason)
        {
            var payment = await _context.Payments
                .Include(p => p.Bill)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == "Completed");
                
            if (payment == null)
            {
                return NotFound();
            }

            var currentUser = _userService.GetCurrentUser(HttpContext);

            payment.Status = "Refunded";
            payment.RefundDate = DateTime.Now;
            payment.RefundedBy = $"{currentUser?.FirstName} {currentUser?.LastName}";
            payment.RefundReason = refundReason;
            
            _context.Update(payment);

            // Update bill status if applicable
            if (payment.BillId.HasValue)
            {
                var bill = await _context.Bills.FindAsync(payment.BillId.Value);
                if (bill != null && bill.Status == "Paid")
                {
                    bill.Status = "Unpaid";
                    _context.Update(bill);
                }
            }

            await _context.SaveChangesAsync();

            // Send notification to user
            await _notificationService.CreateNotificationAsync(
                payment.UserId,
                "Payment Refunded",
                $"Your payment of ${payment.Amount} has been refunded. Reason: {refundReason}",
                NotificationType.Payment,
                NotificationPriority.Normal,
                DeliveryMethod.InApp,
                payment.Id
            );
            
            TempData["SuccessMessage"] = "Payment refunded successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Payments/ViewReceipt/5
        [Authorize(Roles = "Homeowner,Administrator,Staff")]
        public async Task<IActionResult> ViewReceipt(int id)
        {
            // Get the current user
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the payment with related data
            var payment = await _context.Payments
                .Include(p => p.Bill)
                .Include(p => p.User)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Payment not found.";
                return RedirectToAction("PaymentHistory");
            }

            // Check if the user is authorized to view this receipt
            if (currentUser.Id != payment.UserId && !User.IsInRole("Administrator") && !User.IsInRole("Staff"))
            {
                TempData["ErrorMessage"] = "You are not authorized to view this receipt.";
                return RedirectToAction("PaymentHistory");
            }

            return View(payment);
        }

        // POST: Payments/RequestRefund/5 - for homeowner to request a refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> RequestRefund(int paymentId, string refundReason, string refundComments)
        {
            var currentUser = _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && 
                                     p.UserId == currentUser.Id && 
                                     p.Status == "Completed");
                
            if (payment == null)
            {
                return NotFound();
            }

            // In a real system, this would create a refund request record
            // For now, we'll just send a notification to admins

            // Create notification for admins
            var admins = await _context.Users
                .Where(u => u.Role == UserRole.Administrator)
                .ToListAsync();

            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    "Refund Request",
                    $"User {currentUser.FirstName} {currentUser.LastName} has requested a refund of ${payment.Amount}. Reason: {refundReason}. Comments: {refundComments}",
                    NotificationType.Payment,
                    NotificationPriority.Normal,
                    DeliveryMethod.InApp,
                    payment.Id
                );
            }

            // Send confirmation to user
            await _notificationService.CreateNotificationAsync(
                currentUser.Id,
                "Refund Request Submitted",
                $"Your refund request for ${payment.Amount} has been submitted and will be reviewed soon.",
                NotificationType.Payment,
                NotificationPriority.Normal,
                DeliveryMethod.InApp,
                payment.Id
            );
            
            TempData["SuccessMessage"] = "Refund request submitted successfully.";
            return RedirectToAction(nameof(Details), new { id = paymentId });
        }

        // GET: Payments/GenerateReport
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
            
            var payments = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status);
            }
            
            var report = await payments.OrderByDescending(p => p.PaymentDate).ToListAsync();
            
            // Calculate summary statistics
            ViewBag.TotalPayments = report.Count;
            ViewBag.TotalAmount = report.Where(p => p.Status == "Completed").Sum(p => p.Amount);
            ViewBag.TotalRefunded = report.Where(p => p.Status == "Refunded").Sum(p => p.Amount);
            
            // Payment method breakdown
            var methodBreakdown = report
                .Where(p => p.Status == "Completed" && p.PaymentMethod != null)
                .GroupBy(p => p.PaymentMethod.Type)
                .Select(g => new { Method = g.Key.ToString(), Count = g.Count(), Amount = g.Sum(p => p.Amount) })
                .ToList();
            ViewBag.PaymentMethodBreakdown = methodBreakdown;
            
            // Get status options for filter
            ViewBag.StatusOptions = new List<string> { "Pending", "Completed", "Failed", "Refunded" };
            
            return View(report);
        }

        // GET: Payments/PrintReceipt/5
        public async Task<IActionResult> PrintReceipt(int? id)
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

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to view this payment
            if (!User.IsInRole("Administrator") && !User.IsInRole("Staff") && payment.UserId != currentUser.Id)
            {
                return Forbid();
            }

            return View(payment);
        }

        // POST: Payments/DownloadReceipt/5
        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(int? id)
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

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Bill)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            // Check if the user has permission to download this receipt
            if (!User.IsInRole("Administrator") && !User.IsInRole("Staff") && payment.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // In a real system, this would generate a PDF receipt
            // For demo purposes, we'll just return a view
            return View("PrintReceipt", payment);
        }

        // Helper method to update bill status
        private async Task UpdateBillStatus(int billId)
        {
            var bill = await _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == billId);
                
            if (bill != null)
            {
                // Calculate total completed payments
                decimal totalPaid = bill.Payments
                    .Where(p => p.Status == "Completed")
                    .Sum(p => p.Amount);
                    
                // Update bill status based on payment
                if (totalPaid >= bill.Amount)
                {
                    bill.Status = "Paid";
                }
                else if (bill.Status == "Paid")
                {
                    bill.Status = "Unpaid";
                }
                
                _context.Update(bill);
            }
        }

        // Helper method to generate a transaction ID
        private string GenerateTransactionId()
        {
            // In a real system, this would come from a payment processor
            // For now, generate something that looks like a transaction ID
            return $"TRX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        // Helper method to check if payment method exists
        private bool PaymentMethodExists(int id)
        {
            return _context.PaymentMethods.Any(e => e.Id == id);
        }
    }
} 