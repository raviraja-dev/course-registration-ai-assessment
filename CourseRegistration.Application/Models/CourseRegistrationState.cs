using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseRegistration.Domain.Entities;

namespace CourseRegistration.Application.Models
{
    public class CourseRegistrationState
    {
        public CourseOffering CourseOffering { get; }

        public Dictionary<int, Learner> Learners { get; } = new();

        public List<Enrollment> Enrollments { get; } = new();

        public List<WaitlistEntry> Waitlist { get; } = new();

        public long NextSequenceNumber { get; set; } = 1;

        public CourseRegistrationState(CourseOffering courseOffering)
        {
            CourseOffering = courseOffering
                ?? throw new ArgumentNullException(nameof(courseOffering));
        }
    }
}
