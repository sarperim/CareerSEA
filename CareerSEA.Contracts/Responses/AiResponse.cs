using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.Responses
{
    public class AIResponse
    {
        public string best_job { get; set; }
        public float match_score { get; set; }
        public List<AIRecommendation> recommendations { get; set; }
    }

    public class AIRecommendation
    {
        public string label { get; set; }
        public float score { get; set; }
    }
}
