using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class UserDailyActivity
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public int MinutesSpent { get; set; }

        // Navigation
        public User User { get; set; }
    }
}
