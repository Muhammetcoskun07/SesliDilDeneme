using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserDailyActivityDto
    {
        public string Id { get; set; } // UUID
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public int MinutesSpent { get; set; }
    }
}
