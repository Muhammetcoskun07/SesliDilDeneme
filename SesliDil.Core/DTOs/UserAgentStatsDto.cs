using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class UserAgentStatsDto
    {
        public string AgentId { get; set; }
        public double TotalMinutes { get; set; }
        public int TotalMessages { get; set; }
        public int TotalWords { get; set; }
        public double AverageWPM { get; set; }
    }
}
