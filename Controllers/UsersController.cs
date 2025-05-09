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
        public async Task<IActionResult> Create(
            [Bind("Username,Email,Password,FirstName,LastName,PhoneNumber,Role,HomeownerId")] User user, 
            string confirmPassword, 
            string? Address = null, 
            string? LotNumber = null, 
            string? BlockNumber = null)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (user.Password != confirmPassword)
            {
                ModelState.AddModelError("", "Password and confirmation password do not match.");
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                return View(user);
            }

            // Check if username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "This username is already taken");
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                return View(user);
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "This email is already in use");
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure the user is active by default
                    user.IsActive = true;
                    
                    // For Homeowner role with no existing Homeowner selected, create a new Homeowner record
                    if (user.Role == UserRole.Homeowner && (user.HomeownerId == null || user.HomeownerId == 0))
                    {
                        // Check if required homeowner fields are provided
                        if (string.IsNullOrEmpty(Address) || string.IsNullOrEmpty(LotNumber) || string.IsNullOrEmpty(BlockNumber))
                        {
                            ModelState.AddModelError("", "Address, Lot Number, and Block Number are required for new Homeowners.");
                            ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                            ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                            return View(user);
                        }

                        // Create new Homeowner
                        var homeowner = new Homeowner
                        {
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            Phone = user.PhoneNumber ?? string.Empty,
                            Address = Address,
                            LotNumber = LotNumber,
                            BlockNumber = BlockNumber,
                            JoinDate = DateTime.Now
                        };

                        _context.Homeowners.Add(homeowner);
                        await _context.SaveChangesAsync();

                        // Assign the new homeowner ID to the user
                        user.HomeownerId = homeowner.Id;
                    }
                    
                    // Create the user
                    if (await _userService.CreateUserAsync(user, user.Password))
                    {
                        TempData["SuccessMessage"] = "User created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // If we reach here, there was a problem with user creation
                        // If we created a homeowner, we should delete it to avoid orphaned records
                        if (user.HomeownerId.HasValue && user.Role == UserRole.Homeowner)
                        {
                            var homeowner = await _context.Homeowners.FindAsync(user.HomeownerId.Value);
                            if (homeowner != null)
                            {
                                _context.Homeowners.Remove(homeowner);
                                await _context.SaveChangesAsync();
                            }
                        }
                        
                        ModelState.AddModelError("", "An error occurred while creating the user.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,FirstName,LastName,PhoneNumber,Role,IsActive,HomeownerId")] User user, string debugField)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if form was submitted (debug field should be present)
            if (debugField != "EditFormSubmitted")
            {
                TempData["ErrorMessage"] = "Form submission error: Debug field missing";
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                return View(user);
            }

            if (id != user.Id)
            {
                TempData["ErrorMessage"] = "ID mismatch error";
                return NotFound();
            }

            // Always log the received data
            var debugInfo = $"Received data - ID: {user.Id}, Username: {user.Username}, FirstName: {user.FirstName}, LastName: {user.LastName}, Role: {user.Role}";
            Console.WriteLine(debugInfo);

            // Remove Password validation errors since we're not updating the password here
            if (!ModelState.IsValid)
            {
                // Remove Password validation errors
                if (ModelState.ContainsKey("Password"))
                {
                    ModelState.Remove("Password");
                }
                
                // Check if model is valid after removing Password validation
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = $"Model validation failed: {errors}";
                    
                    ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                    ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                    return View(user);
                }
            }

            try
            {
                // Get the existing user from the database
                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found in database";
                    return NotFound();
                }

                // Check for username uniqueness
                if (user.Username != existingUser.Username)
                {
                    var usernameExists = await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id);
                    if (usernameExists)
                    {
                        ModelState.AddModelError("Username", "This username is already taken");
                        ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                        ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                        return View(user);
                    }
                }

                // Check for email uniqueness
                if (user.Email != existingUser.Email)
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id);
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                        ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                        return View(user);
                    }
                }

                // Update the properties manually but preserve the password
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.Role = user.Role;
                existingUser.IsActive = user.IsActive;
                existingUser.HomeownerId = user.HomeownerId;
                // Password is not updated here - we keep the existing password

                // Save changes directly to the database
                var rowsAffected = await _context.SaveChangesAsync();
                
                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = $"User updated successfully. ({rowsAffected} rows affected)";
                }
                else
                {
                    TempData["ErrorMessage"] = "No changes were saved to the database";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await UserExists(user.Id))
                {
                    TempData["ErrorMessage"] = "User no longer exists in database";
                    return NotFound();
                }
                else
                {
                    TempData["ErrorMessage"] = $"Concurrency error: {ex.Message}";
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating user: {ex.Message}";
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["Homeowners"] = new SelectList(await _context.Homeowners.ToListAsync(), "Id", "LastName");
                return View(user);
            }
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

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Actually delete the user from the database
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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
                var user = await _userService.GetUserByIdAsync(id);
                if (user != null)
                {
                    ViewBag.UserId = id;
                    ViewBag.UserName = user.Username;
                }
                return View();
            }

            var userToUpdate = await _userService.GetUserByIdAsync(id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Use the UserService to update the password properly with hashing
            await _userService.ChangePasswordAsync(id, null, newPassword);
            
            // Add success message
            TempData["SuccessMessage"] = $"Password for {userToUpdate.Username} has been changed successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(int id)
        {
            return await _userService.GetUserByIdAsync(id) != null;
        }
    }
} 