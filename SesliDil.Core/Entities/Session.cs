using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Session
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
