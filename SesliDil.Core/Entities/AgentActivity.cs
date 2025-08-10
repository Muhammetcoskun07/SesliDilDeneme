using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.Entities
{
    public class AgentActivity
    {
        public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        public int MessageCount { get; set; } = 0;
    }
}
