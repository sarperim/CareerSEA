using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.Requests
{
    public class AIRequest
    {
        // Python expects "jobs" as a list
        public List<AIJobDto> jobs { get; set; }
    }

    public class AIJobDto
    {
        public string title { get; set; }
        public string description { get; set; }
        public string skills { get; set; }
    }
}
