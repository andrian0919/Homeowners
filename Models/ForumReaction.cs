using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeownersSubdivision.Models
{
    public class ForumReaction
    {
        public int Id { get; set; }

        [Required]
        public ReactionType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign keys
        public int PostId { get; set; }
        public int UserId { get; set; }

        // Navigation properties
        [ForeignKey("PostId")]
        public virtual ForumPost Post { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 