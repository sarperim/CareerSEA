using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.DTOs
{
    public class JobListingDto
    {
        public string Title { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public string Link { get; set; }
    }
}
