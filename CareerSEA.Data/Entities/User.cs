using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }

        public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
