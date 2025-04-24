using Microsoft.AspNetCore.Mvc;
using HomeownersSubdivision.Models;
using HomeownersSubdivision.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using HomeownersSubdivision.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using MySqlConnector;
using System.Data.Common;

namespace HomeownersSubdivision.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        public AccountController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            var isAuthenticated = await _userService.AuthenticateAsync(username, password);
            if (!isAuthenticated)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            // Get user details for the session
            var user = await _userService.GetUserByUsernameAsync(username) ?? await _userService.GetUserByEmailAsync(username);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            // Store user information in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Username);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());
            HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            // Check if user is logged in
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Home");
            }

            // Get user data
            var user = await _context.Users
                .Include(u => u.Homeowner)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            // Debug info to check what's actually in the database
            if (user.Role == UserRole.Homeowner)
            {
                // Get connection string
                var connectionString = _context.Database.GetConnectionString();
                
                // Store debug information
                TempData["UserId"] = user.Id;
                TempData["HomeownerId"] = user.HomeownerId;
                
                // Check if Homeowner actually exists
                if (user.Homeowner != null)
                {
                    TempData["HomeownerActualId"] = user.Homeowner.Id;
                    TempData["HomeownerDebugInfo"] = $"FirstName: {user.Homeowner.FirstName}, " +
                                                    $"LastName: {user.Homeowner.LastName}, " +
                                                    $"Email: {user.Homeowner.Email}, " +
                                                    $"Address: {user.Homeowner.Address ?? "[null]"}, " +
                                                    $"LotNumber: {user.Homeowner.LotNumber ?? "[null]"}, " +
                                                    $"BlockNumber: {user.Homeowner.BlockNumber ?? "[null]"}";
                    
                    // Try direct query to verify data
                    try
                    {
                        using (var connection = new MySqlConnector.MySqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            
                            using (var command = new MySqlConnector.MySqlCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = "SELECT * FROM Homeowners WHERE Id = @Id";
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Id", user.Homeowner.Id));
                                
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        TempData["DirectQueryResult"] = $"Found in DB: " +
                                                                      $"Id={reader["Id"]}, " +
                                                                      $"FirstName={reader["FirstName"]}, " +
                                                                      $"Address={reader["Address"]}";
                                    }
                                    else
                                    {
                                        TempData["DirectQueryResult"] = "Homeowner record not found in database!";
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["DirectQueryError"] = ex.Message;
                    }
                }
                else
                {
                    TempData["HomeownerDebugInfo"] = "Homeowner is null!";
                }
            }

            // Create appropriate view model based on user role
            if (user.Role == UserRole.Homeowner && user.Homeowner != null)
            {
                var viewModel = new HomeownerProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Homeowner.Address,
                    LotNumber = user.Homeowner.LotNumber,
                    BlockNumber = user.Homeowner.BlockNumber,
                    JoinDate = user.Homeowner.JoinDate
                };
                
                return View("HomeownerProfile", viewModel);
            }
            else if (user.Role == UserRole.Administrator)
            {
                var viewModel = new AdminProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber
                };
                
                return View("AdminProfile", viewModel);
            }
            else if (user.Role == UserRole.Staff)
            {
                var viewModel = new StaffProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber
                };
                
                return View("StaffProfile", viewModel);
            }
            
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New password and confirmation password do not match.");
                return View();
            }

            if (await _userService.ChangePasswordAsync(userId.Value, currentPassword, newPassword))
            {
                ViewBag.SuccessMessage = "Password changed successfully.";
                return View();
            }
            else
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View();
            }
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            [Bind("Username,Email,Password,FirstName,LastName,PhoneNumber")] User user, 
            string confirmPassword, 
            string Address, 
            string LotNumber, 
            string BlockNumber)
        {
            if (ModelState.IsValid)
            {
                if (user.Password != confirmPassword)
                {
                    ModelState.AddModelError("", "Password and confirmation password do not match.");
                    return View(user);
                }

                // Create homeowner record first
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

                // By default, new registrations are for homeowners
                user.Role = UserRole.Homeowner;
                user.IsActive = true;
                user.HomeownerId = homeowner.Id;

                if (await _userService.CreateUserAsync(user, user.Password))
                {
                    TempData["SuccessMessage"] = "Registration successful! You can now login.";
                    return RedirectToAction("Login");
                }
                else
                {
                    // Remove the homeowner if user creation failed
                    _context.Homeowners.Remove(homeowner);
                    await _context.SaveChangesAsync();
                    
                    ModelState.AddModelError("", "Username or email already exists.");
                }
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHomeownerProfile(HomeownerProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["DebugErrors"] = string.Join(", ", errors);
                return View("HomeownerProfile", model);
            }

            try
            {
                // Get user data with homeowner included
                var user = await _context.Users
                    .Include(u => u.Homeowner)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User record not found.";
                    return View("HomeownerProfile", model);
                }

                // Check if homeowner data exists
                if (user.Homeowner == null || user.HomeownerId == null)
                {
                    TempData["ErrorMessage"] = "Homeowner record not found. The user account might not be properly linked to a homeowner record.";
                    return View("HomeownerProfile", model);
                }

                // Store IDs for debugging
                TempData["UserId"] = user.Id;
                TempData["HomeownerId"] = user.HomeownerId;
                TempData["HomeownerActualId"] = user.Homeowner.Id;

                // Validate username uniqueness only if it changed
                if (user.Username != model.Username)
                {
                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.Username == model.Username && u.Id != model.Id);
                    
                    if (usernameExists)
                    {
                        ModelState.AddModelError("Username", "This username is already taken");
                        return View("HomeownerProfile", model);
                    }
                }

                // Validate email uniqueness only if it changed
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);
                    
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View("HomeownerProfile", model);
                    }
                }

                // Get the connection string
                var connectionString = _context.Database.GetConnectionString();
                
                // TRANSACTION APPROACH: Use a single transaction to ensure both updates succeed or fail together
                using (var connection = new MySqlConnector.MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Start a transaction
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // First, update the User table
                            using (var command = new MySqlConnector.MySqlCommand())
                            {
                                command.Connection = connection;
                                command.Transaction = transaction;
                                command.CommandText = @"
                                    UPDATE Users 
                                    SET Username = @Username,
                                        Email = @Email,
                                        FirstName = @FirstName,
                                        LastName = @LastName,
                                        PhoneNumber = @PhoneNumber
                                    WHERE Id = @Id";
                                
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Username", model.Username));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Email", model.Email));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@FirstName", model.FirstName));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@LastName", model.LastName));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@PhoneNumber", model.PhoneNumber ?? (object)DBNull.Value));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Id", model.Id));
                                
                                var userRowsAffected = await command.ExecuteNonQueryAsync();
                                TempData["UserUpdateResult"] = $"User update affected {userRowsAffected} rows";
                            }

                            // Then, update the Homeowner table - use the actual Homeowner.Id (not the foreign key)
                            using (var command = new MySqlConnector.MySqlCommand())
                            {
                                command.Connection = connection;
                                command.Transaction = transaction;
                                command.CommandText = @"
                                    UPDATE Homeowners 
                                    SET FirstName = @FirstName,
                                        LastName = @LastName,
                                        Email = @Email,
                                        Phone = @Phone,
                                        Address = @Address,
                                        LotNumber = @LotNumber,
                                        BlockNumber = @BlockNumber
                                    WHERE Id = @Id";
                                
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@FirstName", model.FirstName));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@LastName", model.LastName));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Email", model.Email));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Phone", model.PhoneNumber ?? string.Empty));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Address", model.Address ?? (object)DBNull.Value));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@LotNumber", model.LotNumber ?? (object)DBNull.Value));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@BlockNumber", model.BlockNumber ?? (object)DBNull.Value));
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Id", user.HomeownerId.Value)); // Use the HomeownerId foreign key
                                
                                var homeownerRowsAffected = await command.ExecuteNonQueryAsync();
                                TempData["HomeownerUpdateResult"] = $"Homeowner update affected {homeownerRowsAffected} rows";
                                
                                // Double check with a SELECT query to make sure it updated
                                command.CommandText = "SELECT Address FROM Homeowners WHERE Id = @Id";
                                command.Parameters.Clear();
                                command.Parameters.Add(new MySqlConnector.MySqlParameter("@Id", user.HomeownerId.Value));
                                
                                var updatedAddress = await command.ExecuteScalarAsync();
                                TempData["VerifyResult"] = $"After update, Address = {updatedAddress ?? "[null]"}";
                            }

                            // Update password if requested
                            if (model.ChangePassword && !string.IsNullOrEmpty(model.NewPassword))
                            {
                                using (var command = new MySqlConnector.MySqlCommand())
                                {
                                    command.Connection = connection;
                                    command.Transaction = transaction;
                                    command.CommandText = "UPDATE Users SET Password = @Password WHERE Id = @Id";
                                    command.Parameters.Add(new MySqlConnector.MySqlParameter("@Password", model.NewPassword));
                                    command.Parameters.Add(new MySqlConnector.MySqlParameter("@Id", model.Id));
                                    
                                    var passwordRowsAffected = await command.ExecuteNonQueryAsync();
                                    TempData["PasswordUpdateResult"] = $"Password update affected {passwordRowsAffected} rows";
                                }
                            }
                            
                            // Commit the transaction
                            await transaction.CommitAsync();
                            TempData["TransactionStatus"] = "Transaction committed successfully";
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            await transaction.RollbackAsync();
                            TempData["TransactionStatus"] = $"Transaction rolled back due to error: {ex.Message}";
                            throw;
                        }
                    }
                }

                // Clear entity tracking to force reload from database
                _context.ChangeTracker.Clear();
                
                // Get updated data
                var updatedUser = await _context.Users
                    .Include(u => u.Homeowner)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);
                    
                if (updatedUser?.Homeowner != null)
                {
                    // Update session data
                    HttpContext.Session.SetString("UserName", updatedUser.Username);
                    HttpContext.Session.SetString("FullName", $"{updatedUser.FirstName} {updatedUser.LastName}");
                    HttpContext.Session.SetString("UserRole", updatedUser.Role.ToString());

                    // Add debug info to success message
                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                    
                    // Add additional debug info
                    TempData["DebugInfo"] = $"Username: {user.Username} → {updatedUser.Username}, " +
                                           $"Email: {user.Email} → {updatedUser.Email}, " +
                                           $"Address: {user.Homeowner.Address ?? "[null]"} → {updatedUser.Homeowner.Address ?? "[null]"}, " +
                                           $"Lot: {user.Homeowner.LotNumber ?? "[null]"} → {updatedUser.Homeowner.LotNumber ?? "[null]"}, " +
                                           $"Block: {user.Homeowner.BlockNumber ?? "[null]"} → {updatedUser.Homeowner.BlockNumber ?? "[null]"}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not verify the update.";
                }

                return View("HomeownerProfile", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    TempData["ErrorDetails"] = ex.InnerException.Message;
                }
                
                return View("HomeownerProfile", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdminProfile(AdminProfileViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["DebugErrors"] = string.Join(", ", errors);
                return View("AdminProfile", model);
            }

            try
            {
                // Get user data
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User record not found.";
                    return NotFound();
                }

                // Validate username uniqueness only if it changed
                if (user.Username != model.Username)
                {
                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.Username == model.Username && u.Id != model.Id);
                    
                    if (usernameExists)
                    {
                        ModelState.AddModelError("Username", "This username is already taken");
                        return View("AdminProfile", model);
                    }
                }

                // Validate email uniqueness only if it changed
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);
                    
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View("AdminProfile", model);
                    }
                }

                // Store original values for debugging
                var originalUsername = user.Username;
                var originalEmail = user.Email;

                // Update user details
                user.Username = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                // Update password if requested
                if (model.ChangePassword && !string.IsNullOrEmpty(model.NewPassword))
                {
                    user.Password = model.NewPassword;  // In a real app, this should be hashed
                }

                // Mark entity as modified to ensure changes are tracked
                _context.Entry(user).State = EntityState.Modified;

                // Save changes and get number of records affected
                var recordsAffected = await _context.SaveChangesAsync();

                // Update session data
                HttpContext.Session.SetString("UserName", user.Username);
                HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
                HttpContext.Session.SetString("UserRole", user.Role.ToString());

                // Add debug info to success message
                TempData["SuccessMessage"] = $"Your profile has been updated successfully. Records affected: {recordsAffected}";
                
                // Add additional debug info
                TempData["DebugInfo"] = $"Username changed from {originalUsername} to {user.Username}. Email changed from {originalEmail} to {user.Email}.";
                
                // If returnUrl is provided, redirect to it
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return View("AdminProfile", model);
            }
            catch (Exception ex)
            {
                // Log the exception details
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    TempData["ErrorDetails"] = ex.InnerException.Message;
                }
                
                return View("AdminProfile", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStaffProfile(StaffProfileViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["DebugErrors"] = string.Join(", ", errors);
                return View("StaffProfile", model);
            }

            try
            {
                // Get user data
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User record not found.";
                    return NotFound();
                }

                // Validate username uniqueness only if it changed
                if (user.Username != model.Username)
                {
                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.Username == model.Username && u.Id != model.Id);
                    
                    if (usernameExists)
                    {
                        ModelState.AddModelError("Username", "This username is already taken");
                        return View("StaffProfile", model);
                    }
                }

                // Validate email uniqueness only if it changed
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);
                    
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View("StaffProfile", model);
                    }
                }

                // Store original values for debugging
                var originalUsername = user.Username;
                var originalEmail = user.Email;

                // Update user details
                user.Username = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                // Update password if requested
                if (model.ChangePassword && !string.IsNullOrEmpty(model.NewPassword))
                {
                    user.Password = model.NewPassword;  // In a real app, this should be hashed
                }

                // Mark entity as modified to ensure changes are tracked
                _context.Entry(user).State = EntityState.Modified;

                // Save changes and get number of records affected
                var recordsAffected = await _context.SaveChangesAsync();

                // Update session data
                HttpContext.Session.SetString("UserName", user.Username);
                HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
                HttpContext.Session.SetString("UserRole", user.Role.ToString());

                // Add debug info to success message
                TempData["SuccessMessage"] = $"Your profile has been updated successfully. Records affected: {recordsAffected}";
                
                // Add additional debug info
                TempData["DebugInfo"] = $"Username changed from {originalUsername} to {user.Username}. Email changed from {originalEmail} to {user.Email}.";
                
                // If returnUrl is provided, redirect to it
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return View("StaffProfile", model);
            }
            catch (Exception ex)
            {
                // Log the exception details
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                
                if (ex.InnerException != null)
                {
                    TempData["ErrorDetails"] = ex.InnerException.Message;
                }
                
                return View("StaffProfile", model);
            }
        }
    }
} 