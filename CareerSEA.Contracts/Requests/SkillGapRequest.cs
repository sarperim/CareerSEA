using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.Requests
{
    public class SkillGapRequest
    {
        public string BestJob { get; set; } = string.Empty;
        public List<string> UserSkills { get; set; } = new();
    }
}
