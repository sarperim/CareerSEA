using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Data.Entities
{
    public class Experience
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } //FK
        public int? ESCOId { get; set; } //FK nullable till i set up esco db and esco title api
        public string Title { get; set; }
        public string Skills { get; set; }
        public string Description { get; set; }


        public User User { get; set; }
        public ESCO_Occupation ESCO_Occupation { get; set; }
    }
}
