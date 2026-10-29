using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseRegistration.Application.Models
{
    public enum EnrollmentStatus
    {
        Enrolled,
        Waitlisted,
        AlreadyEnrolled,
        AlreadyWaitlisted,
        PrerequisiteNotMet,
        InvalidLearner

    }
}
