using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SystemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: /System/SeedForumCategories
        public async Task<IActionResult> SeedForumCategories()
        {
            try
            {
                // Check if any categories exist
                if (!await _context.ForumCategories.AnyAsync())
                {
                    // Create default categories
                    var categories = new List<ForumCategory>
                    {
                        new ForumCategory 
                        { 
                            Name = "General Discussion", 
                            Description = "Discuss any general topics related to our community", 
                            DisplayOrder = 1,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Announcements", 
                            Description = "Important community announcements and updates", 
                            DisplayOrder = 2,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Events & Activities", 
                            Description = "Discuss upcoming events and activities in our neighborhood", 
                            DisplayOrder = 3,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Facilities", 
                            Description = "Discussions about our community facilities and amenities", 
                            DisplayOrder = 4,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Help & Support", 
                            Description = "Ask questions and get help from community members", 
                            DisplayOrder = 5,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        }
                    };
                    
                    await _context.ForumCategories.AddRangeAsync(categories);
                    await _context.SaveChangesAsync();
                    
                    return Content("Forum categories have been created successfully!");
                }
                
                return Content("Forum categories already exist.");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }
    }
}