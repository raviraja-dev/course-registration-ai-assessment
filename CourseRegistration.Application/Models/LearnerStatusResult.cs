using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Application.Models
{
    public class LearnerStatusResult
    {
        public int LearnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public int? WaitlistPosition { get; set; }
    }
}
