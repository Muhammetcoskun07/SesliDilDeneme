using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class Progress
    {
        public int ProgressId { get; set; }
        public int UserId { get; set; } //Foreign Key
        public string CurrentLevel { get; set; }
        public string TargetLevel { get; set; }
        public decimal ProgressRate { get; set; }
        public User User { get; set; }

    }
}
