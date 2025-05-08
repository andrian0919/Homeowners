using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HomeownersSubdivision.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace HomeownersSubdivision.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Check user role from session
            var userRole = HttpContext.Session.GetString("UserRole");
            
            // Redirect to appropriate dashboard based on role
            if (!string.IsNullOrEmpty(userRole))
            {
                if (userRole == UserRole.Administrator.ToString())
                {
                    return RedirectToAction("AdminDashboard");
                }
                else if (userRole == UserRole.Staff.ToString())
                {
                    return RedirectToAction("StaffDashboard");
                }
                else if (userRole == UserRole.Homeowner.ToString())
                {
                    return RedirectToAction("HomeownerDashboard");
                }
            }
            
            // Default view for unauthenticated users
            return View();
        }

        public async Task<IActionResult> HomeownerDashboard()
        {
            // Check if user is a homeowner
            if (HttpContext.Session.GetString("UserRole") != UserRole.Homeowner.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Set luxury background
            ViewData["UseLuxuryBackground"] = true;
            
            // Get recent announcements for the homeowner dashboard
            var today = DateTime.Today;
            var announcements = await _context.Announcements
                .Include(a => a.Creator)
                .Where(a => a.IsPublished && (a.ExpireDate == null || a.ExpireDate >= today))
                .OrderByDescending(a => a.PublishDate)
                .Take(5) // Get the 5 most recent announcements
                .ToListAsync();
            
            // Pass announcements to the view
            ViewBag.Announcements = announcements;
            
            return View();
        }

        public async Task<IActionResult> AdminDashboard()
        {
            // Check if user is an admin
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Set viewdata for dynamic admin background
            ViewData["UseAdminBackground"] = true;
            
            // Get statistics for admin dashboard
            ViewBag.TotalHomeowners = await _context.Homeowners.CountAsync();
            ViewBag.StaffMembers = await _context.Users.CountAsync(u => u.Role == UserRole.Staff);
            ViewBag.MonthlyRevenue = await _context.Payments
                .Where(p => p.PaymentDate.Month == DateTime.Now.Month && p.PaymentDate.Year == DateTime.Now.Year)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            ViewBag.PendingRequests = await _context.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.New);
            
            return View();
        }

        public IActionResult StaffDashboard()
        {
            // Check if user is staff
            if (HttpContext.Session.GetString("UserRole") != UserRole.Staff.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            return View();
        }

        public IActionResult Login()
        {
            // If user is already logged in, redirect to home page
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Index");
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username and password match a user in the database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);
                
                if (user != null)
                {
                    // Store user information in session
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Username);
                    HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
                    HttpContext.Session.SetString("UserRole", user.Role.ToString());
                    
                    // Create claims for the user
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString()),
                        new System.Security.Claims.Claim("UserId", user.Id.ToString()),
                        new System.Security.Claims.Claim("FullName", $"{user.FirstName} {user.LastName}")
                    };

                    // Create the identity and principal
                    var identity = new System.Security.Claims.ClaimsIdentity(claims, "CookieAuth");
                    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                    // Sign in the user
                    await HttpContext.SignInAsync("CookieAuth", principal);
                    
                    // Redirect based on role
                    if (user.Role == UserRole.Administrator)
                    {
                        return RedirectToAction("AdminDashboard");
                    }
                    else if (user.Role == UserRole.Staff)
                    {
                        return RedirectToAction("StaffDashboard");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                
                ModelState.AddModelError("", "Invalid username or password");
            }
            
            return View(model);
        }

        public IActionResult Register()
        {
            // If user is already logged in, redirect to home page
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Index");
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);
                
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }
                
                // Create homeowner record first
                var homeowner = new Homeowner
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Phone = model.PhoneNumber ?? string.Empty,
                    LotNumber = model.LotNumber,
                    BlockNumber = model.BlockNumber,
                    JoinDate = DateTime.Now
                };
                
                _context.Homeowners.Add(homeowner);
                await _context.SaveChangesAsync();
                
                // Create new user
                var newUser = new User
                {
                    Username = model.Username,
                    Password = model.Password, // In a real app, hash this password
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Role = UserRole.Homeowner, // New registrations are homeowners by default
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    HomeownerId = homeowner.Id
                };
                
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                
                // Instead of logging in the user automatically, redirect to login page
                // with a success message
                TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            
            return View(model);
        }

        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            
            // Sign out the user
            HttpContext.SignOutAsync("CookieAuth");
            
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            // Set luxury background
            ViewData["UseLuxuryBackground"] = true;
            
            // Populate the About page with system info
            ViewBag.SystemName = "NeighborHood Subdivision Management System";
            ViewBag.Version = "2.0";
            ViewBag.DeveloperName = "HomeownersSubdivision Development Team";
            ViewBag.EstablishmentYear = "2023";
            
            // Company information
            ViewBag.CompanyName = "NeighborHood Subdivision";
            ViewBag.CompanyAddress = "123 Community Drive, Subdivision City";
            ViewBag.CompanyPhone = "(555) 123-4567";
            ViewBag.CompanyEmail = "info@neighborhoodsub.com";
            
            // Set features
            ViewBag.Features = new List<string>
            {
                "Homeowner Management",
                "Billing and Payments",
                "Facility Reservations",
                "Service Requests",
                "Community Forum",
                "Visitor Passes",
                "Vehicle Registration",
                "Announcements",
                "Emergency Contacts",
                "Reports and Analytics"
            };
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            var errorViewModel = new ErrorViewModel 
            { 
                RequestId = requestId
            };
            
            // Check if we're in development environment
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            ViewBag.ShowDevInfo = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
            
            return View(errorViewModel);
        }
    }
} 