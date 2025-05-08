using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.Services
{
    public interface IForumService
    {
        // Category operations
        Task<List<ForumCategory>> GetAllCategoriesAsync();
        Task<ForumCategory> GetCategoryByIdAsync(int id);
        Task<ForumCategory> CreateCategoryAsync(ForumCategory category);
        Task<ForumCategory> UpdateCategoryAsync(ForumCategory category);
        Task<bool> DeleteCategoryAsync(int id);
        
        // Topic operations
        Task<ForumTopic> GetTopicDetailsAsync(int id, bool incrementViewCount = true);
        Task<List<ForumTopic>> GetTopicsByCategoryAsync(int categoryId, int page = 1, int pageSize = 20);
        Task<ForumTopic> CreateTopicAsync(ForumTopic topic, string postContent);
        Task<ForumTopic> UpdateTopicAsync(ForumTopic topic);
        Task<bool> DeleteTopicAsync(int id);
        Task<bool> PinTopicAsync(int id);
        Task<bool> UnpinTopicAsync(int id);
        Task<bool> LockTopicAsync(int id);
        Task<bool> UnlockTopicAsync(int id);
        
        // Post operations
        Task<ForumPost> GetPostAsync(int id);
        Task<List<ForumPost>> GetPostsByTopicAsync(int topicId, int page = 1, int pageSize = 20);
        Task<ForumPost> CreatePostAsync(ForumPost post);
        Task<ForumPost> UpdatePostAsync(ForumPost post);
        Task<bool> DeletePostAsync(int id);
        Task<List<ForumPost>> GetRepliesForPostAsync(int postId);
        Task<ForumPost> CreateReplyAsync(ForumPost reply);
        
        // Reaction operations
        Task<ForumReaction> AddReactionAsync(ForumReaction reaction);
        Task<bool> RemoveReactionAsync(int reactionId);
        Task<Dictionary<ReactionType, int>> GetReactionSummaryForPostAsync(int postId);
        Task<List<ForumReaction>> GetReactionsByUserAsync(int userId);
        
        // Statistics and home page
        Task<Dictionary<string, object>> GetForumIndexDataAsync();
        Task<int> GetTotalTopicsCountAsync();
        Task<int> GetTotalPostsCountAsync();
        Task<List<ForumTopic>> GetRecentTopicsAsync(int count = 5);
        Task<List<ForumTopic>> GetPopularTopicsAsync(int count = 5);
        
        // Search
        Task<List<ForumTopic>> SearchTopicsAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<List<ForumPost>> SearchPostsAsync(string searchTerm, int page = 1, int pageSize = 20);
        
        // User-specific
        Task<List<ForumTopic>> GetTopicsByUserAsync(int userId, int page = 1, int pageSize = 20);
        Task<List<ForumPost>> GetPostsByUserAsync(int userId, int page = 1, int pageSize = 20);
        Task<bool> HasUserReactedToPostAsync(int userId, int postId, ReactionType? reactionType = null);
    }
} 