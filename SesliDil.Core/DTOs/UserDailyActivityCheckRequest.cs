using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserDailyActivityCheckRequest
    {
        public string UserId { get; set; }
        public List<DateTime>? Dates { get; set; }
        public List<DayOfWeek>? Days { get; set; }
    }
}
