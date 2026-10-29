using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Domain.Entities
{
    public  class Enrollment
    {
        public int LearnerId {  get; private set; }

        public Enrollment(int learnerId) 
        {
            if (learnerId <= 0) throw new ArgumentOutOfRangeException(nameof(learnerId), "LearnerId must be greater than Zero.");
            LearnerId = learnerId;
        }
    }
}
