using System.Collections.Generic;

namespace HomeownersSubdivision.Models
{
    public class ForumIndexData
    {
        public List<ForumCategory> Categories { get; set; } = new List<ForumCategory>();
        public List<ForumTopic> RecentTopics { get; set; } = new List<ForumTopic>();
        public int TotalTopics { get; set; }
        public int TotalPosts { get; set; }
        public int TotalMembers { get; set; }
    }
} 