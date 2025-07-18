using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class ProgressDto
    {
        public int ProgressId { get; set; }
        public int UserId { get; set; }
        public string CurrentLevel { get; set; }
        public string TargetLevel { get; set; }
        public decimal ProgressRate { get; set; }
    }
}
