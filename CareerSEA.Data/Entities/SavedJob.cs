using System;

namespace CareerSEA.Data.Entities
{
    public class SavedJob
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
