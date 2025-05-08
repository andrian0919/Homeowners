using System;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HomeownersSubdivision.Controllers
{
    public class ContactDirectoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactDirectoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ContactDirectory
        public async Task<IActionResult> Index()
        {
            var userRoleString = HttpContext.Session.GetString("UserRole");
            
            // Default to homeowner if not logged in or role not found
            var userRole = UserRole.Homeowner;
            
            if (!string.IsNullOrEmpty(userRoleString))
            {
                Enum.TryParse(userRoleString, out userRole);
            }
            
            int roleFlag = GetRoleFlagFromUserRole(userRole);
            
            var contacts = await _context.ContactDirectory
                .Where(c => c.IsActive && (c.VisibleToRoles & roleFlag) != 0)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
                
            ViewBag.UserRole = userRole;
            
            return View(contacts);
        }
        
        // GET: ContactDirectory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.ContactDirectory
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (contact == null)
            {
                return NotFound();
            }
            
            // Check if the current user has permission to view this contact
            var userRoleString = HttpContext.Session.GetString("UserRole");
            var userRole = UserRole.Homeowner; // Default
            
            if (!string.IsNullOrEmpty(userRoleString))
            {
                Enum.TryParse(userRoleString, out userRole);
            }
            
            if (!contact.IsVisibleToRole(userRole))
            {
                return Forbid();
            }

            return View(contact);
        }
        
        // GET: ContactDirectory/Create
        public IActionResult Create()
        {
            // Only administrators can create contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            return View();
        }
        
        // POST: ContactDirectory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Title,Department,Category,PhoneNumber,Email,Office,Address,Notes,WorkingHours,SortOrder,VisibleToRoles")] ContactDirectory contact)
        {
            // Only administrators can create contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (ModelState.IsValid)
            {
                contact.IsActive = true;
                contact.CreatedAt = DateTime.Now;
                contact.CreatedById = HttpContext.Session.GetInt32("UserId");
                
                _context.Add(contact);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Index));
            }
            
            return View(contact);
        }
        
        // GET: ContactDirectory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Only administrators can edit contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.ContactDirectory.FindAsync(id);
            
            if (contact == null)
            {
                return NotFound();
            }
            
            return View(contact);
        }
        
        // POST: ContactDirectory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Title,Department,Category,PhoneNumber,Email,Office,Address,Notes,WorkingHours,SortOrder,VisibleToRoles,IsActive")] ContactDirectory contact)
        {
            // Only administrators can edit contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingContact = await _context.ContactDirectory.FindAsync(id);
                    
                    if (existingContact == null)
                    {
                        return NotFound();
                    }
                    
                    // Update fields
                    existingContact.Name = contact.Name;
                    existingContact.Title = contact.Title;
                    existingContact.Department = contact.Department;
                    existingContact.Category = contact.Category;
                    existingContact.PhoneNumber = contact.PhoneNumber;
                    existingContact.Email = contact.Email;
                    existingContact.Office = contact.Office;
                    existingContact.Address = contact.Address;
                    existingContact.Notes = contact.Notes;
                    existingContact.WorkingHours = contact.WorkingHours;
                    existingContact.SortOrder = contact.SortOrder;
                    existingContact.VisibleToRoles = contact.VisibleToRoles;
                    existingContact.IsActive = contact.IsActive;
                    existingContact.UpdatedAt = DateTime.Now;
                    existingContact.UpdatedById = HttpContext.Session.GetInt32("UserId");
                    
                    _context.Update(existingContact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactDirectoryExists(contact.Id))
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
            
            return View(contact);
        }
        
        // GET: ContactDirectory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            // Only administrators can delete contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.ContactDirectory
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }
        
        // POST: ContactDirectory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Only administrators can delete contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var contact = await _context.ContactDirectory.FindAsync(id);
            
            if (contact != null)
            {
                // Soft delete - just mark as inactive
                contact.IsActive = false;
                contact.UpdatedAt = DateTime.Now;
                contact.UpdatedById = HttpContext.Session.GetInt32("UserId");
                
                _context.Update(contact);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: ContactDirectory/Manage
        public async Task<IActionResult> Manage()
        {
            // Only administrators can manage all contacts
            if (HttpContext.Session.GetString("UserRole") != UserRole.Administrator.ToString())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var contacts = await _context.ContactDirectory
                .Include(c => c.CreatedBy)
                .Include(c => c.UpdatedBy)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
                
            return View(contacts);
        }
        
        private bool ContactDirectoryExists(int id)
        {
            return _context.ContactDirectory.Any(e => e.Id == id);
        }
        
        private int GetRoleFlagFromUserRole(UserRole role)
        {
            switch(role)
            {
                case UserRole.Administrator:
                    return 1;
                case UserRole.Staff:
                    return 2;
                case UserRole.Homeowner:
                    return 4;
                default:
                    return 4; // Default to homeowner permissions
            }
        }
    }
} 