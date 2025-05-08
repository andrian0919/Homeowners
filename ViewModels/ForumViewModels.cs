using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HomeownersSubdivision.Models;

namespace HomeownersSubdivision.ViewModels
{
    public class ForumIndexViewModel
    {
        public List<ForumCategoryViewModel> Categories { get; set; } = new List<ForumCategoryViewModel>();
        public List<ForumTopicViewModel> RecentTopics { get; set; } = new List<ForumTopicViewModel>();
        public List<ForumTopicViewModel> PopularTopics { get; set; } = new List<ForumTopicViewModel>();
        public int TotalTopics { get; set; }
        public int TotalPosts { get; set; }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public List<ForumTopicViewModel> Topics { get; set; } = new List<ForumTopicViewModel>();
        public required PaginationViewModel Pagination { get; set; }
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }

    public class TopicViewModel
    {
        public required ForumTopic Topic { get; set; }
        public required List<ForumPost> Posts { get; set; }
    }

    public class CreateTopicViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 5)]
        public required string Title { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [StringLength(500)]
        public required string Description { get; set; }

        [Required]
        [StringLength(10000, MinimumLength = 10)]
        public required string Content { get; set; }
    }

    public class CreatePostViewModel
    {
        [Required]
        public int TopicId { get; set; }

        public int? ParentPostId { get; set; }

        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public required string Content { get; set; }
    }

    public class SearchViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Query { get; set; }
    }

    public class ForumCategoryViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int DisplayOrder { get; set; }
        public int TopicCount { get; set; }
        public int PostCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public required string LastTopicTitle { get; set; }
        public int? LastTopicId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ForumTopicViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public required ForumPostViewModel LastPost { get; set; }
    }

    public class ForumPostViewModel
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public required string TopicTitle { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorAvatarUrl { get; set; }
        public int? ParentPostId { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public List<ForumPostViewModel> Replies { get; set; } = new List<ForumPostViewModel>();
        public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new Dictionary<ReactionType, int>();
        public ReactionType? UserReaction { get; set; }
    }

    public class ForumSearchViewModel
    {
        public required string Query { get; set; }
        public List<ForumTopicViewModel> Results { get; set; } = new List<ForumTopicViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalResults { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
    }
} 