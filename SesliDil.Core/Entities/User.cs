using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Language { get; set; }
        public string Interests { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int Streak { get; set; }

        public ICollection<Progress> Progresses {  get; set; }

    }
}
