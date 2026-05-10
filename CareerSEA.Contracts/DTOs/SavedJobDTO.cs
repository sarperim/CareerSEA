using System;

namespace CareerSEA.Contracts.DTOs
{
    public class SavedJobDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}
