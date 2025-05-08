using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class ForumTopic
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public int ViewCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;

        public bool IsPinned { get; set; } = false;

        public bool IsLocked { get; set; } = false;
        
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime LastActivityAt { get; set; } = DateTime.Now;

        // Foreign keys
        public int CategoryId { get; set; }
        public int CreatedById { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual ForumCategory Category { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
    }
} 