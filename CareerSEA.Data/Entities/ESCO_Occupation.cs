using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Data.Entities
{
    public class ESCO_Occupation
    {
        [Key]
        public int ESCOId { get; set; }
        public string Title { get; set; }

        public ICollection<Experience> Experiences { get; set; }
    }
}
