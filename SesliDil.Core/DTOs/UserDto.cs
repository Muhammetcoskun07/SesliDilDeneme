using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Language { get; set; }
        public string Interests { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
