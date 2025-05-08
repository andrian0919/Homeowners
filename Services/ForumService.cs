using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeownersSubdivision.Data;
using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeownersSubdivision.Services
{
    public class ForumService : IForumService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ForumService> _logger;
        private readonly IUserService _userService;

        public ForumService(ApplicationDbContext context, ILogger<ForumService> logger, IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        #region Category Operations
        public async Task<List<ForumCategory>> GetAllCategoriesAsync()
        {
            return await _context.ForumCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<ForumCategory> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.ForumCategories
                .Include(c => c.Topics.Where(t => t.IsActive))
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);
        }

        public async Task<ForumCategory> CreateCategoryAsync(ForumCategory category)
        {
            category.CreatedAt = DateTime.Now;
            category.UpdatedAt = DateTime.Now;
            
            _context.ForumCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<ForumCategory> UpdateCategoryAsync(ForumCategory category)
        {
            var existingCategory = await _context.ForumCategories.FindAsync(category.Id);
            if (existingCategory == null)
                return null;

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.DisplayOrder = category.DisplayOrder;
            existingCategory.IsActive = category.IsActive;
            existingCategory.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return existingCategory;
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.ForumCategories.FindAsync(categoryId);
            if (category == null)
                return false;

            category.IsActive = false;
            category.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Topic Operations
        public async Task<ForumTopic> GetTopicDetailsAsync(int topicId, bool incrementViewCount = true)
        {
            var topic = await _context.ForumTopics
                .Include(t => t.CreatedBy)
                .Include(t => t.Category)
                .Include(t => t.Posts.Where(p => p.IsActive && p.ParentPostId == null))
                    .ThenInclude(p => p.CreatedBy)
                .Include(t => t.Posts.Where(p => p.IsActive && p.ParentPostId == null))
                    .ThenInclude(p => p.Reactions)
                .Include(t => t.Posts.Where(p => p.IsActive && p.ParentPostId == null))
                    .ThenInclude(p => p.Replies.Where(r => r.IsActive))
                        .ThenInclude(r => r.CreatedBy)
                .Include(t => t.Posts.Where(p => p.IsActive && p.ParentPostId == null))
                    .ThenInclude(p => p.Replies.Where(r => r.IsActive))
                        .ThenInclude(r => r.Reactions)
                .FirstOrDefaultAsync(t => t.Id == topicId && t.IsActive);

            if (topic != null && incrementViewCount)
            {
                topic.ViewCount++;
                await _context.SaveChangesAsync();
            }

            return topic;
        }

        public async Task<List<ForumTopic>> GetTopicsByCategoryAsync(int categoryId, int page = 1, int pageSize = 20)
        {
            return await _context.ForumTopics
                .Where(t => t.CategoryId == categoryId && t.IsActive)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.LastActivityAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ForumTopic> CreateTopicAsync(ForumTopic topic, string postContent)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                topic.LastActivityAt = DateTime.Now;
                _context.ForumTopics.Add(topic);
                await _context.SaveChangesAsync();

                // Create initial post
                var post = new ForumPost
                {
                    Content = postContent,
                    TopicId = topic.Id,
                    CreatedById = topic.CreatedById
                };
                _context.ForumPosts.Add(post);
                
                // Update reply count
                topic.ReplyCount = 1;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return topic;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ForumTopic> UpdateTopicAsync(ForumTopic topic)
        {
            _context.ForumTopics.Update(topic);
            await _context.SaveChangesAsync();
            return topic;
        }

        public async Task<bool> DeleteTopicAsync(int topicId)
        {
            var topic = await _context.ForumTopics.FindAsync(topicId);
            if (topic == null)
                return false;

            topic.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PinTopicAsync(int topicId)
        {
            var topic = await _context.ForumTopics.FindAsync(topicId);
            if (topic == null)
                return false;

            topic.IsPinned = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnpinTopicAsync(int topicId)
        {
            var topic = await _context.ForumTopics.FindAsync(topicId);
            if (topic == null)
                return false;

            topic.IsPinned = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockTopicAsync(int topicId)
        {
            var topic = await _context.ForumTopics.FindAsync(topicId);
            if (topic == null)
                return false;

            topic.IsLocked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockTopicAsync(int topicId)
        {
            var topic = await _context.ForumTopics.FindAsync(topicId);
            if (topic == null)
                return false;

            topic.IsLocked = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PinTopicAsync(int topicId, bool isPinned)
        {
            if (isPinned)
                return await PinTopicAsync(topicId);
            else
                return await UnpinTopicAsync(topicId);
        }

        public async Task<bool> LockTopicAsync(int topicId, bool isLocked)
        {
            if (isLocked)
                return await LockTopicAsync(topicId);
            else
                return await UnlockTopicAsync(topicId);
        }
        #endregion

        #region Post Operations
        public async Task<ForumPost> GetPostAsync(int postId)
        {
            return await _context.ForumPosts
                .Include(p => p.CreatedBy)
                .Include(p => p.Topic)
                .Include(p => p.Reactions)
                .Include(p => p.Replies.Where(r => r.IsActive))
                    .ThenInclude(r => r.CreatedBy)
                .Include(p => p.Replies.Where(r => r.IsActive))
                    .ThenInclude(r => r.Reactions)
                .FirstOrDefaultAsync(p => p.Id == postId && p.IsActive);
        }

        public async Task<List<ForumPost>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.ForumPosts
                    .Where(p => p.TopicId == topicId && p.IsActive && p.ParentPostId == null)
                    .OrderBy(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.Reactions)
                    .Include(p => p.Replies.Where(r => r.IsActive))
                    .ThenInclude(r => r.Reactions)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts for topic ID: {TopicId}", topicId);
                throw;
            }
        }

        public async Task<ForumPost> CreatePostAsync(ForumPost post)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ForumPosts.Add(post);
                await _context.SaveChangesAsync();

                // Update topic's LastActivityAt and ReplyCount
                var topic = await _context.ForumTopics.FindAsync(post.TopicId);
                if (topic != null)
                {
                    topic.LastActivityAt = DateTime.Now;
                    topic.ReplyCount++;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return post;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ForumPost> UpdatePostAsync(ForumPost post)
        {
            post.UpdatedAt = DateTime.Now;
            post.IsEdited = true;
            _context.ForumPosts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
                return false;

            post.IsActive = false;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkPostAsAnswerAsync(int postId, bool isAnswer)
        {
            var post = await _context.ForumPosts
                .Include(p => p.Topic)
                .FirstOrDefaultAsync(p => p.Id == postId);
                
            if (post == null)
                return false;

            post.IsAnswer = isAnswer;
            post.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ForumPost>> GetRepliesForPostAsync(int postId)
        {
            try
            {
                return await _context.ForumPosts
                    .Where(p => p.ParentPostId == postId && p.IsActive)
                    .Include(p => p.CreatedBy)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving replies for post ID: {PostId}", postId);
                throw;
            }
        }

        public async Task<ForumPost> CreateReplyAsync(ForumPost reply)
        {
            try
            {
                _context.ForumPosts.Add(reply);
                await _context.SaveChangesAsync();
                
                // Update topic's LastActivityAt
                var topic = await _context.ForumTopics.FindAsync(reply.TopicId);
                if (topic != null)
                {
                    topic.LastActivityAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                
                return reply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply to post ID: {PostId}", reply.ParentPostId);
                throw;
            }
        }
        #endregion

        #region Reaction Operations
        public async Task<ForumReaction> AddReactionAsync(ForumReaction reaction)
        {
            try
            {
                var existingReaction = await _context.ForumReactions
                    .FirstOrDefaultAsync(r => r.PostId == reaction.PostId && r.UserId == reaction.UserId);

                if (existingReaction != null)
                {
                    existingReaction.Type = reaction.Type;
                    existingReaction.CreatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return existingReaction;
                }

                _context.ForumReactions.Add(reaction);
                await _context.SaveChangesAsync();
                return reaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction to post ID: {PostId}", reaction.PostId);
                throw;
            }
        }

        public async Task<ForumReaction> AddReactionAsync(int postId, int userId, ReactionType type)
        {
            var existingReaction = await _context.ForumReactions
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId && r.Type == type);

            if (existingReaction != null)
            {
                _context.ForumReactions.Remove(existingReaction);
                await _context.SaveChangesAsync();
                return null;
            }

            var reaction = new ForumReaction
            {
                PostId = postId,
                UserId = userId,
                Type = type
            };

            _context.ForumReactions.Add(reaction);
            await _context.SaveChangesAsync();
            return reaction;
        }

        public async Task<bool> RemoveReactionAsync(int reactionId)
        {
            try
            {
                var reaction = await _context.ForumReactions.FindAsync(reactionId);
                if (reaction == null)
                    return false;

                _context.ForumReactions.Remove(reaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction ID: {ReactionId}", reactionId);
                throw;
            }
        }

        public async Task<Dictionary<ReactionType, int>> GetReactionSummaryForPostAsync(int postId)
        {
            var reactions = await _context.ForumReactions
                .Where(r => r.PostId == postId)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<ReactionType, int>();
            foreach (var reactionType in Enum.GetValues<ReactionType>())
            {
                result[reactionType] = 0;
            }

            foreach (var reaction in reactions)
            {
                result[reaction.Type] = reaction.Count;
            }

            return result;
        }

        public async Task<List<ForumReaction>> GetReactionsByUserAsync(int userId)
        {
            try
            {
                return await _context.ForumReactions
                    .Where(r => r.UserId == userId)
                    .Include(r => r.Post)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reactions for user ID: {UserId}", userId);
                throw;
            }
        }
        #endregion

        #region Statistics and Home Page
        public async Task<Dictionary<string, object>> GetForumIndexDataAsync()
        {
            var categories = await _context.ForumCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    Category = c,
                    TopicCount = c.Topics.Count(t => t.IsActive),
                    PostCount = c.Topics.Where(t => t.IsActive).Sum(t => t.ReplyCount),
                    LatestTopic = c.Topics
                        .Where(t => t.IsActive)
                        .OrderByDescending(t => t.LastActivityAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var recentTopics = await _context.ForumTopics
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.LastActivityAt)
                .Take(5)
                .Include(t => t.CreatedBy)
                .Include(t => t.Category)
                .ToListAsync();

            var result = new Dictionary<string, object>
            {
                ["Categories"] = categories,
                ["RecentTopics"] = recentTopics,
                ["TotalTopics"] = await _context.ForumTopics.CountAsync(t => t.IsActive),
                ["TotalPosts"] = await _context.ForumPosts.CountAsync(p => p.IsActive)
            };

            return result;
        }

        public async Task<int> GetTotalTopicsCountAsync()
        {
            return await _context.ForumTopics.CountAsync(t => t.IsActive);
        }

        public async Task<int> GetTotalPostsCountAsync()
        {
            return await _context.ForumPosts.CountAsync(p => p.IsActive);
        }
        
        public async Task<List<ForumTopic>> GetRecentTopicsAsync(int count = 10)
        {
            try
            {
                return await _context.ForumTopics
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.LastActivityAt)
                    .Take(count)
                    .Include(t => t.Category)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent topics");
                throw;
            }
        }
        
        public async Task<List<ForumTopic>> GetPopularTopicsAsync(int count = 10)
        {
            try
            {
                return await _context.ForumTopics
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.ViewCount)
                    .Take(count)
                    .Include(t => t.Category)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular topics");
                throw;
            }
        }
        #endregion

        #region Search
        public async Task<List<ForumTopic>> SearchTopicsAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<ForumTopic>();

            return await _context.ForumTopics
                .Where(t => t.IsActive && 
                           (t.Title.Contains(searchTerm) || 
                            t.Description.Contains(searchTerm) || 
                            t.Posts.Any(p => p.IsActive && p.Content.Contains(searchTerm))))
                .Include(t => t.CreatedBy)
                .Include(t => t.Category)
                .OrderByDescending(t => t.LastActivityAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ForumPost>> SearchPostsAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return new List<ForumPost>();
                    
                return await _context.ForumPosts
                    .Where(p => p.IsActive && p.Content.Contains(searchTerm))
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(p => p.Topic)
                    .Include(p => p.CreatedBy)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts for: {SearchTerm}", searchTerm);
                throw;
            }
        }
        #endregion

        #region User-Specific Methods
        public async Task<List<ForumTopic>> GetTopicsByUserAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.ForumTopics
                .Where(t => t.CreatedById == userId && t.IsActive)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ForumPost>> GetPostsByUserAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.ForumPosts
                .Where(p => p.CreatedById == userId && p.IsActive)
                .Include(p => p.Topic)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ForumTopic>> GetParticipatedTopicsAsync(int userId)
        {
            var topicIds = await _context.ForumPosts
                .Where(p => p.CreatedById == userId && p.IsActive)
                .Select(p => p.TopicId)
                .Distinct()
                .ToListAsync();

            return await _context.ForumTopics
                .Where(t => topicIds.Contains(t.Id) && t.IsActive)
                .Include(t => t.Category)
                .OrderByDescending(t => t.LastActivityAt)
                .ToListAsync();
        }

        public async Task<bool> SubscribeToTopicAsync(int userId, int topicId)
        {
            // Placeholder implementation - replace with actual implementation once subscription model is created
            return true;
        }

        public async Task<bool> UnsubscribeFromTopicAsync(int userId, int topicId)
        {
            // Placeholder implementation - replace with actual implementation once subscription model is created
            return true;
        }

        public async Task<bool> HasUserReactedToPostAsync(int userId, int postId, ReactionType? reactionType = null)
        {
            var query = _context.ForumReactions
                .Where(r => r.UserId == userId && r.PostId == postId);
                
            if (reactionType.HasValue)
            {
                query = query.Where(r => r.Type == reactionType.Value);
            }
            
            return await query.AnyAsync();
        }
        #endregion
    }
} 