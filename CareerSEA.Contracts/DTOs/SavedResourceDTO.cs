using System;
using System.Collections.Generic;

namespace CareerSEA.Contracts.DTOs
{
    public class SavedResourceDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Skill { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }

    public class SavedResourceGroupDTO
    {
        public string Skill { get; set; } = string.Empty;
        public List<SavedResourceDTO> Resources { get; set; } = new();
    }

    public class SavedItemsResponse
    {
        public List<SavedJobDTO> Jobs { get; set; } = new();
        public List<SavedResourceGroupDTO> Resources { get; set; } = new();
    }
}
