using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Application.Models
{
    public class EnrollmentResult
    {
        public EnrollmentStatus Status{ get; }
        public string Message{ get; }
        public int? WaitlistPosition {  get; }

        public EnrollmentResult(EnrollmentStatus status, string message, int? waitlistPosition)
        {
            if(string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "Message cannot be null or whitespace.",
                    nameof(message));
            };
            Status = status;
            Message = message;
            WaitlistPosition = waitlistPosition;
        }
    }
}
