using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using System.Security.Claims;
using HomeownersSubdivision.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HomeownersSubdivision.Services;

namespace HomeownersSubdivision.Controllers
{
    [Authorize] // Require login for all forum functions
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ForumController> _logger;
        private readonly IForumService _forumService;

        public ForumController(
            ApplicationDbContext context, 
            ILogger<ForumController> logger,
            IForumService forumService)
        {
            _context = context;
            _logger = logger;
            _forumService = forumService;
        }

        // GET: /Forum
        public async Task<IActionResult> Index()
        {
            try
            {
                // Check if categories exist, if not create defaults
                if (!await _context.ForumCategories.AnyAsync())
                {
                    var categories = new List<ForumCategory>
                    {
                        new ForumCategory 
                        { 
                            Name = "General Discussion", 
                            Description = "Talk about anything related to our community", 
                            DisplayOrder = 1,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Announcements", 
                            Description = "Important announcements from management", 
                            DisplayOrder = 2,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Events", 
                            Description = "Upcoming events and activities", 
                            DisplayOrder = 3,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new ForumCategory 
                        { 
                            Name = "Facilities", 
                            Description = "Discussions about our facilities", 
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
                    
                    _logger.LogInformation("Created default forum categories");
                }
                
                // Now get forum data
                var forumData = await _forumService.GetForumIndexDataAsync();
                
                // Map to view models
                var recentTopics = ((List<ForumTopic>)forumData["RecentTopics"]).Select(t => new ForumTopicViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    AuthorId = t.CreatedById.ToString(),
                    AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.LastActivityAt,
                    ViewCount = t.ViewCount,
                    ReplyCount = t.ReplyCount,
                    IsPinned = t.IsPinned,
                    IsLocked = t.IsLocked,
                    LastPost = new ForumPostViewModel {
                        Id = 0,
                        TopicId = t.Id,
                        TopicTitle = t.Title,
                        AuthorId = t.CreatedById.ToString(),
                        AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                        AuthorAvatarUrl = "/images/avatar.jpg",
                        Content = "",
                        CreatedAt = t.CreatedAt
                    }
                }).ToList();
                
                // Extract categories from the complex data structure
                var categoriesData = forumData["Categories"] as dynamic;
                var categoryViewModels = new List<ForumCategoryViewModel>();
                
                foreach (var item in categoriesData)
                {
                    var category = item.Category as ForumCategory;
                    var latestTopic = item.LatestTopic as ForumTopic;
                    categoryViewModels.Add(new ForumCategoryViewModel
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Description = category.Description,
                        DisplayOrder = category.DisplayOrder,
                        TopicCount = item.TopicCount,
                        PostCount = item.PostCount,
                        IsActive = category.IsActive,
                        LastTopicTitle = latestTopic != null ? latestTopic.Title : "No topics yet"
                    });
                }
                
                return View(new HomeownersSubdivision.ViewModels.ForumIndexViewModel
                {
                    Categories = categoryViewModels,
                    RecentTopics = recentTopics,
                    PopularTopics = new List<ForumTopicViewModel>(), // Populate if needed
                    TotalTopics = (int)forumData["TotalTopics"],
                    TotalPosts = (int)forumData["TotalPosts"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/Index");
                return View("Error");
            }
        }

        // GET: /Forum/Category/5
        public async Task<IActionResult> Category(int id, int page = 1)
        {
            try
            {
                var category = await _forumService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                
                var topics = await _forumService.GetTopicsByCategoryAsync(id, page);
                var pageSize = 20; // Same as in service
                
                // Map to view models
                var topicViewModels = topics.Select(t => new ForumTopicViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    CategoryId = t.CategoryId,
                    CategoryName = category.Name,
                    AuthorId = t.CreatedById.ToString(),
                    AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.LastActivityAt,
                    ViewCount = t.ViewCount,
                    ReplyCount = t.ReplyCount,
                    IsPinned = t.IsPinned,
                    IsLocked = t.IsLocked,
                    LastPost = new ForumPostViewModel {
                        Id = 0,
                        TopicId = t.Id,
                        TopicTitle = t.Title,
                        AuthorId = t.CreatedById.ToString(),
                        AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                        AuthorAvatarUrl = "/images/avatar.jpg",
                        Content = "",
                        CreatedAt = t.LastActivityAt
                    }
                }).ToList();
                
                var model = new HomeownersSubdivision.ViewModels.CategoryViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    Topics = topicViewModels,
                    Pagination = new HomeownersSubdivision.ViewModels.PaginationViewModel
                    {
                        CurrentPage = page,
                        TotalPages = (int)Math.Ceiling((double)category.Topics.Count / pageSize),
                        HasPreviousPage = page > 1,
                        HasNextPage = page < (int)Math.Ceiling((double)category.Topics.Count / pageSize)
                    }
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/Category");
                return View("Error");
            }
        }

        // GET: /Forum/Topic/5
        public async Task<IActionResult> Topic(int id, int page = 1)
        {
            try
            {
                var topic = await _forumService.GetTopicDetailsAsync(id);
                if (topic == null)
                {
                    return NotFound();
                }
                
                var posts = await _forumService.GetPostsByTopicAsync(id, page);
                var pageSize = 20; // Same as in service
                
                // Map to view models
                var postViewModels = posts.Select(p => new ForumPostViewModel
                {
                    Id = p.Id,
                    TopicId = p.TopicId,
                    TopicTitle = topic.Title,
                    AuthorId = p.CreatedById.ToString(),
                    AuthorName = p.CreatedBy?.FullName ?? "Unknown",
                    AuthorAvatarUrl = "/images/avatar.jpg", // Default avatar
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsEdited = p.IsEdited,
                    Replies = p.Replies.Select(r => new ForumPostViewModel
                    {
                        Id = r.Id,
                        TopicId = r.TopicId,
                        TopicTitle = topic.Title,
                        ParentPostId = r.ParentPostId,
                        AuthorId = r.CreatedById.ToString(),
                        AuthorName = r.CreatedBy?.FullName ?? "Unknown",
                        AuthorAvatarUrl = "/images/avatar.jpg",
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        IsEdited = r.IsEdited
                    }).ToList(),
                    ReactionCounts = GetReactionCountsForPost(p)
                }).ToList();
                
                var topicViewModel = new ForumTopicViewModel
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    CategoryId = topic.CategoryId,
                    CategoryName = topic.Category?.Name ?? "Unknown",
                    AuthorId = topic.CreatedById.ToString(),
                    AuthorName = topic.CreatedBy?.FullName ?? "Unknown",
                    CreatedAt = topic.CreatedAt,
                    LastActivityAt = topic.LastActivityAt,
                    ViewCount = topic.ViewCount,
                    ReplyCount = topic.ReplyCount,
                    IsPinned = topic.IsPinned,
                    IsLocked = topic.IsLocked,
                    LastPost = new ForumPostViewModel {
                        Id = 0,
                        TopicId = topic.Id,
                        TopicTitle = topic.Title,
                        AuthorId = topic.CreatedById.ToString(),
                        AuthorName = topic.CreatedBy?.FullName ?? "Unknown",
                        AuthorAvatarUrl = "/images/avatar.jpg",
                        Content = "",
                        CreatedAt = topic.LastActivityAt
                    }
                };
                
                var model = new HomeownersSubdivision.ViewModels.TopicViewModel
                {
                    Topic = topic,
                    Posts = posts
                };
                
                // Add reply form if needed
                ViewBag.ReplyForm = new CreatePostViewModel { 
                    TopicId = topic.Id,
                    Content = ""
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/Topic");
                return View("Error");
            }
        }

        private Dictionary<ReactionType, int> GetReactionCountsForPost(ForumPost post)
        {
            var reactionCounts = new Dictionary<ReactionType, int>();
            
            var reactionGroups = post.Reactions
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() });
                
            foreach (var group in reactionGroups)
            {
                reactionCounts[group.Type] = group.Count;
            }
            
            return reactionCounts;
        }

        // GET: /Forum/CreateTopic/5 (Category ID)
        [HttpGet]
        public async Task<IActionResult> CreateTopic(int id)
        {
            try
            {
                var category = await _forumService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                
                return View(new CreateTopicViewModel { 
                    CategoryId = id,
                    Title = "",
                    Description = "",
                    Content = ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/CreateTopic");
                return View("Error");
            }
        }

        // POST: /Forum/CreateTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopic(CreateTopicViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    if (userId == 0)
                    {
                        return RedirectToAction("Login", "Account");
                    }
                    
                    var topic = new ForumTopic
                    {
                        Title = model.Title,
                        Description = string.IsNullOrEmpty(model.Description) ? string.Empty : model.Description,
                        CategoryId = model.CategoryId,
                        CreatedById = userId,
                        CreatedAt = DateTime.Now,
                        LastActivityAt = DateTime.Now,
                        IsActive = true
                    };
                    
                    var createdTopic = await _forumService.CreateTopicAsync(topic, model.Content);
                    
                    return RedirectToAction(nameof(Topic), new { id = createdTopic.Id });
                }
                
                // If we got this far, something failed - redisplay form
                var category = await _context.ForumCategories.FindAsync(model.CategoryId);
                ViewBag.CategoryName = category?.Name ?? "Unknown Category";
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/CreateTopic POST");
                TempData["ErrorMessage"] = "An error occurred while creating your topic. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Forum/CreatePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CreatePostViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                    if (userId == 0)
                    {
                        return RedirectToAction("Login", "Account");
                    }
                    
                    // Check if the topic exists and is not locked
                    var topic = await _context.ForumTopics.FindAsync(model.TopicId);
                    if (topic == null)
                    {
                        return NotFound();
                    }
                    
                    if (topic.IsLocked)
                    {
                        TempData["ErrorMessage"] = "This topic is locked and cannot be replied to.";
                        return RedirectToAction(nameof(Topic), new { id = model.TopicId });
                    }
                    
                    var post = new ForumPost
                    {
                        Content = model.Content,
                        TopicId = model.TopicId,
                        CreatedById = userId,
                        ParentPostId = model.ParentPostId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsActive = true
                    };
                    
                    await _forumService.CreatePostAsync(post);
                    
                    return RedirectToAction(nameof(Topic), new { id = model.TopicId });
                }
                
                TempData["ErrorMessage"] = "There was an error with your post. Please check the form and try again.";
                return RedirectToAction(nameof(Topic), new { id = model.TopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/CreatePost POST");
                TempData["ErrorMessage"] = "An error occurred while creating your post. Please try again.";
                return RedirectToAction(nameof(Topic), new { id = model.TopicId });
            }
        }

        // POST: /Forum/AddReaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReaction(int postId, ReactionType reactionType)
        {
            try
            {
                var post = await _context.ForumPosts
                    .Where(p => p.Id == postId)
                    .Include(p => p.Topic)
                    .FirstOrDefaultAsync();
                    
                if (post == null)
                {
                    return NotFound();
                }
                
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Check if user already reacted with this reaction
                var existingReaction = await _context.ForumReactions
                    .Where(r => r.PostId == postId && r.UserId == userId && r.Type == reactionType)
                    .FirstOrDefaultAsync();
                    
                if (existingReaction != null)
                {
                    // Remove the reaction if it already exists (toggle)
                    _context.ForumReactions.Remove(existingReaction);
                }
                else
                {
                    // Add the new reaction
                    var reaction = new ForumReaction
                    {
                        PostId = postId,
                        UserId = userId,
                        Type = reactionType,
                        CreatedAt = DateTime.Now
                    };
                    
                    await _forumService.AddReactionAsync(reaction);
                }
                
                return RedirectToAction(nameof(Topic), new { id = post.TopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/AddReaction POST");
                TempData["ErrorMessage"] = "An error occurred while adding the reaction. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Forum/EditPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["ErrorMessage"] = "Post content cannot be empty.";
                    return RedirectToAction(nameof(Index));
                }
                
                var post = await _context.ForumPosts
                    .Where(p => p.Id == id)
                    .Include(p => p.Topic)
                    .FirstOrDefaultAsync();
                    
                if (post == null)
                {
                    return NotFound();
                }
                
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0 || post.CreatedById != userId)
                {
                    TempData["ErrorMessage"] = "You can only edit your own posts.";
                    return RedirectToAction(nameof(Topic), new { id = post.TopicId });
                }
                
                post.Content = content;
                post.UpdatedAt = DateTime.Now;
                post.IsEdited = true;
                
                await _forumService.UpdatePostAsync(post);
                
                TempData["SuccessMessage"] = "Your post has been updated.";
                return RedirectToAction(nameof(Topic), new { id = post.TopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/EditPost POST");
                TempData["ErrorMessage"] = "An error occurred while editing your post. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Forum/LockTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> LockTopic(int id)
        {
            try
            {
                var topic = await _context.ForumTopics.FindAsync(id);
                if (topic == null)
                {
                    return NotFound();
                }
                
                topic.IsLocked = !topic.IsLocked; // Toggle
                await _forumService.UpdateTopicAsync(topic);
                
                TempData["SuccessMessage"] = topic.IsLocked ? 
                    "Topic has been locked." : 
                    "Topic has been unlocked.";
                    
                return RedirectToAction(nameof(Topic), new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/LockTopic POST");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Forum/PinTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> PinTopic(int id)
        {
            try
            {
                var topic = await _context.ForumTopics.FindAsync(id);
                if (topic == null)
                {
                    return NotFound();
                }
                
                topic.IsPinned = !topic.IsPinned; // Toggle
                await _forumService.UpdateTopicAsync(topic);
                
                TempData["SuccessMessage"] = topic.IsPinned ? 
                    "Topic has been pinned." : 
                    "Topic has been unpinned.";
                    
                return RedirectToAction(nameof(Topic), new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/PinTopic POST");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Forum/Search
        [HttpGet]
        public async Task<IActionResult> Search(string query, int page = 1)
        {
            const int PageSize = 20;
            
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return View(new ForumSearchViewModel { Query = "" });
                }
                
                var topics = await _forumService.SearchTopicsAsync(query, page, PageSize);
                
                // Map to view models
                var results = topics.Select(t => new ForumTopicViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category?.Name ?? "Unknown",
                    AuthorId = t.CreatedById.ToString(),
                    AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                    CreatedAt = t.CreatedAt,
                    LastActivityAt = t.LastActivityAt,
                    ViewCount = t.ViewCount,
                    ReplyCount = t.ReplyCount,
                    IsPinned = t.IsPinned,
                    IsLocked = t.IsLocked,
                    LastPost = new ForumPostViewModel {
                        Id = 0,
                        TopicId = t.Id,
                        TopicTitle = t.Title,
                        AuthorId = t.CreatedById.ToString(),
                        AuthorName = t.CreatedBy?.FullName ?? "Unknown",
                        AuthorAvatarUrl = "/images/avatar.jpg",
                        Content = "",
                        CreatedAt = t.LastActivityAt
                    }
                }).ToList();
                
                var model = new ForumSearchViewModel
                {
                    Query = query,
                    Results = results,
                    CurrentPage = page,
                    PageSize = PageSize,
                    TotalResults = results.Count // This is not accurate for pagination
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/Search");
                return View("Error");
            }
        }

        // GET: /Forum/ManageCategories
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ManageCategories()
        {
            try
            {
                var categories = await _forumService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/ManageCategories");
                return View("Error");
            }
        }

        // GET: /Forum/CreateCategory
        [Authorize(Roles = "Administrator")]
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: /Forum/CreateCategory
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(ForumCategory category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    category.CreatedAt = DateTime.Now;
                    await _forumService.CreateCategoryAsync(category);
                    
                    TempData["SuccessMessage"] = "Category created successfully.";
                    return RedirectToAction(nameof(ManageCategories));
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/CreateCategory POST");
                ModelState.AddModelError("", "An error occurred while creating the category.");
                return View(category);
            }
        }

        // GET: /Forum/EditCategory/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _forumService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: /Forum/EditCategory/5
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, ForumCategory category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    category.UpdatedAt = DateTime.Now;
                    
                    var updatedCategory = await _forumService.UpdateCategoryAsync(category);
                    if (updatedCategory == null)
                    {
                        return NotFound();
                    }
                    
                    TempData["SuccessMessage"] = "Category updated successfully.";
                    return RedirectToAction(nameof(ManageCategories));
                }
                return View(category);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/EditCategory POST");
                ModelState.AddModelError("", "An error occurred while updating the category.");
                return View(category);
            }
        }

        // POST: /Forum/DeleteCategory/5
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var success = await _forumService.DeleteCategoryAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                
                TempData["SuccessMessage"] = "Category deleted successfully.";
                return RedirectToAction(nameof(ManageCategories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Forum/DeleteCategory POST");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
                return RedirectToAction(nameof(ManageCategories));
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.ForumCategories.Any(e => e.Id == id);
        }
    }
} 