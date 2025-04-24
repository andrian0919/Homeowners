using Microsoft.AspNetCore.Mvc;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;

namespace HomeownersSubdivision.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public UsersController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // Check if user is logged in and has admin role
        private bool IsAdmin()
        {
            // For simplicity, using session to check if user is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == UserRole.Administrator.ToString();
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Email,Password,FirstName,LastName,PhoneNumber,Role,HomeownerId")] User user, string confirmPassword)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                if (await _userService.CreateUserAsync(user, user.Password))
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Username or email already exists.");
                }
            }

            ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,FirstName,LastName,PhoneNumber,Role,IsActive,HomeownerId")] User user)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _userService.GetUserByIdAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Update only these properties
                    existingUser.Username = user.Username;
                    existingUser.Email = user.Email;
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Role = user.Role;
                    existingUser.IsActive = user.IsActive;
                    existingUser.HomeownerId = user.HomeownerId;

                    await _userService.UpdateUserAsync(existingUser);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            await _userService.DeleteUserAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ChangePassword/5
        public async Task<IActionResult> ChangePassword(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserId = id;
            ViewBag.UserName = user.Username;
            return View();
        }

        // POST: Users/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation password do not match.");
                return View();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Admin can change password without knowing current password
            user.Password = newPassword;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(int id)
        {
            return await _userService.GetUserByIdAsync(id) != null;
        }
    }
} 