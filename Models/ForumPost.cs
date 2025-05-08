using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class ForumPost
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public bool IsEdited { get; set; } = false;

        public bool IsAnswer { get; set; } = false;
        
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Foreign keys
        public int TopicId { get; set; }
        public int CreatedById { get; set; }
        public int? ParentPostId { get; set; }

        // Navigation properties
        [ForeignKey("TopicId")]
        public virtual ForumTopic Topic { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        [ForeignKey("ParentPostId")]
        public virtual ForumPost ParentPost { get; set; }

        public virtual ICollection<ForumPost> Replies { get; set; } = new List<ForumPost>();
        public virtual ICollection<ForumReaction> Reactions { get; set; } = new List<ForumReaction>();
    }
} 