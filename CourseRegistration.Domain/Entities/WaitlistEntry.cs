using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Domain.Entities
{
    public class WaitlistEntry
    {
        public int LearnerId {  get; private set; }
        public long SequenceNumber {  get; private set; }

        public WaitlistEntry(int learnerId, long sequenceNumber)
        {
            if (learnerId <= 0) throw new ArgumentOutOfRangeException(nameof(learnerId), "LearnerId must be greater than Zero.");
            if (sequenceNumber <= 0) throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "SequenceNumber must be greater than Zero.");
            LearnerId = learnerId;
            SequenceNumber = sequenceNumber;
        }


    }
}
