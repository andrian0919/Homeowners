using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Homeowners.ViewModels
{
    public class ForumIndexViewModel
    {
        public List<ForumCategoryViewModel> Categories { get; set; } = new List<ForumCategoryViewModel>();
        public int TotalTopics { get; set; }
        public int TotalPosts { get; set; }
        public List<RecentTopicViewModel> RecentTopics { get; set; } = new List<RecentTopicViewModel>();
        public List<PopularTopicViewModel> PopularTopics { get; set; } = new List<PopularTopicViewModel>();
    }

    public class ForumCategoryViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int TopicCount { get; set; }
        public int PostCount { get; set; }
        public DateTime? LastActivity { get; set; }
        public required string LastActivityBy { get; set; }
        public required string LastTopicTitle { get; set; }
        public int LastTopicId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ForumCategoryDetailsViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string CreatedBy { get; set; }
        public List<ForumTopicListViewModel> Topics { get; set; } = new List<ForumTopicListViewModel>();
        public required PaginationInfo Pagination { get; set; }
    }

    public class ForumTopicListViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public required string LastActivityBy { get; set; }
    }

    public class RecentTopicViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public required string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReplyCount { get; set; }
    }

    public class PopularTopicViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
    }

    public class TopicDetailsViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public int ViewCount { get; set; }
        public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
        public required PaginationInfo Pagination { get; set; }
        public required CreatePostViewModel ReplyForm { get; set; }
    }

    public class PostViewModel
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public required string Content { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsAnswer { get; set; }
        public int ParentPostId { get; set; }
        public List<ReactionSummaryViewModel> Reactions { get; set; } = new List<ReactionSummaryViewModel>();
        public List<PostViewModel> Replies { get; set; } = new List<PostViewModel>();
    }

    public class ReactionSummaryViewModel
    {
        public required string Type { get; set; }
        public int Count { get; set; }
        public bool HasReacted { get; set; }
    }

    public class CreateTopicViewModel
    {
        public int CategoryId { get; set; }
        
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public required string Title { get; set; }
        
        [StringLength(1000)]
        public required string Description { get; set; }
        
        [Required]
        [StringLength(10000, MinimumLength = 10)]
        public required string Content { get; set; }
        
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }

    public class CreatePostViewModel
    {
        public int TopicId { get; set; }
        
        public int? ParentPostId { get; set; }
        
        [Required]
        [StringLength(10000, MinimumLength = 5)]
        public required string Content { get; set; }
    }

    public class EditPostViewModel
    {
        public int Id { get; set; }
        
        public int TopicId { get; set; }
        
        [Required]
        [StringLength(10000, MinimumLength = 5)]
        public required string Content { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);
    }
} 