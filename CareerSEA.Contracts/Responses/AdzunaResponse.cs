using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.Responses
{
        public class AdzunaResponse
        {
            public List<AdzunaJob> Results { get; set; } = new();
        }

        public class AdzunaJob
        {
            public string Title { get; set; }

            [JsonPropertyName("redirect_url")]
            public string Url { get; set; }

            public AdzunaCompany Company { get; set; }
            public AdzunaLocation Location { get; set; }
        }

        public class AdzunaCompany { [JsonPropertyName("display_name")] public string Name { get; set; } }
        public class AdzunaLocation { [JsonPropertyName("display_name")] public string Name { get; set; } }
    }
