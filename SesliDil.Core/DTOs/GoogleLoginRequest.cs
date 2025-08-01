using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
        public bool HasCompletedOnboarding { get; set; }
    }
}
