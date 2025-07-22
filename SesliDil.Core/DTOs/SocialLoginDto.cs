using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class SocialLoginDto
    {
        public string Provider { get; set; }
        public string IdToken { get; set; }
    }
}
