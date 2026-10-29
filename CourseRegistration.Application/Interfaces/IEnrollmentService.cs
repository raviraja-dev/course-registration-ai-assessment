using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseRegistration.Application.Models;

namespace CourseRegistration.Application.Interfaces
{
    public interface IEnrollmentService
    {
        EnrollmentResult Enroll(int learnerId);

        DropResult Drop(int learnerId);
        LearnerStatusResult? GetLearnerStatus(int learnerId);
    }
}
