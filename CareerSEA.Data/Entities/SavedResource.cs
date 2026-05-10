using System;

namespace CareerSEA.Data.Entities
{
    public class SavedResource
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Skill { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
