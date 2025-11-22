using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Data.Entities
{
    public class Prediction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } //FK
        public double Guess { get; set; }
        public string InputVector { get; set; }


        public User User { get; set; }
    }
}
