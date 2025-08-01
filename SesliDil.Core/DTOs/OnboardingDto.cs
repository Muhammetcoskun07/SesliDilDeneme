using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SesliDil.Core.DTOs
{
    public class OnboardingDto
    {
        public string NativeLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string ProficiencyLevel { get; set; }
        public string AgeRange { get; set; }
        public bool HasCompletedOnboarding { get; set; }
    }
}

