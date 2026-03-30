using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.Requests
{
    public class ResourceRecommendationRequest
    {
        public string BestJob { get; set; } = string.Empty;
        public List<string> MissingSkills { get; set; } = new();
        public List<string> UserSkills { get; set; } = new();

        public int MaxSkills { get; set; } = 5;
        public int PerSkill { get; set; } = 4;
    }
}
