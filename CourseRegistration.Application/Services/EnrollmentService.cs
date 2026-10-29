using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseRegistration.Application.Interfaces;
using CourseRegistration.Application.Models;
using CourseRegistration.Domain.Entities;

namespace CourseRegistration.Application.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ICourseRepository _courseRepository;
        public EnrollmentService(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public EnrollmentResult Enroll(int learnerId)
        {
            if (learnerId <= 0)
            {
                return new EnrollmentResult(EnrollmentStatus.InvalidLearner, "Learner ID must be greater than zero.", null);
            }
            return _courseRepository.ExecuteAtomically(state =>
            {
                if (!state.Learners.TryGetValue(learnerId, out var learner))
                {
                    return new EnrollmentResult(EnrollmentStatus.InvalidLearner, "Learner not found.", null);
                }
                if (state.Enrollments.Any(e => e.LearnerId == learnerId))
                {
                    return new EnrollmentResult(EnrollmentStatus.AlreadyEnrolled, "Learner is already enrolled.", null);
                }

                if (state.Waitlist.Any(e => e.LearnerId == learnerId))
                {
                    return new EnrollmentResult(EnrollmentStatus.AlreadyWaitlisted, "Learner is already in waitinglist.", null);
                }
                if (!learner.HasMetPrerequisite)
                {
                    return new EnrollmentResult(EnrollmentStatus.PrerequisiteNotMet, "Learner has not met the course prerequisite.", null);
                }
                if (state.Enrollments.Count < state.CourseOffering.Capacity)
                {
                    var enrollement = new Enrollment(learnerId);

                    state.Enrollments.Add(enrollement);

                    return new EnrollmentResult(EnrollmentStatus.Enrolled, "You have sucessfully Enrolled.", null);
                }
                else
                {
                    var waitinglist = new WaitlistEntry(learnerId, state.NextSequenceNumber);
                    state.Waitlist.Add(waitinglist);
                    state.NextSequenceNumber++;
                    int position = state.Waitlist.Count;
                    return new EnrollmentResult(EnrollmentStatus.Waitlisted, "Your placed in queue.", position);
                }
            });

        }

        public DropResult Drop(int learnerId)
        {
            if (learnerId <= 0)
            {
                return new DropResult(DropStatus.InvalidLearner, "Learner ID must be greater than zero.", null);
            }
            return _courseRepository.ExecuteAtomically(state =>
            {
                var enrollment = state.Enrollments.FirstOrDefault(e => e.LearnerId == learnerId);

                if (enrollment == null)
                {
                    return new DropResult(DropStatus.NotEnrolled, "Learner is not enrolled.", null);
                }
                state.Enrollments.Remove(enrollment);
                var earliestWaitlistEntry = state.Waitlist.OrderBy(entry => entry.SequenceNumber).FirstOrDefault(entry => state.Learners.TryGetValue(entry.LearnerId, out var learner) && learner.HasMetPrerequisite);
                if (earliestWaitlistEntry != null)
                {
                    state.Waitlist.Remove(earliestWaitlistEntry);

                    var promotedEnrollment = new Enrollment(
                        earliestWaitlistEntry.LearnerId);

                    state.Enrollments.Add(promotedEnrollment);

                    return new DropResult(DropStatus.Dropped, "Learner dropped and waitlisted learner promoted.", earliestWaitlistEntry.LearnerId);
                }
                return new DropResult(DropStatus.Dropped, "Learner dropped successfully.", null);
            });
        }



        public LearnerStatusResult? GetLearnerStatus(int learnerId)
        {
            if (learnerId <= 0)
                return null;

            return _courseRepository.ExecuteAtomically(state =>
            {
                if (!state.Learners.TryGetValue(learnerId, out var learner))
                    return null;

                string status = "NotRegistered";
                int? position = null;

                if (state.Enrollments.Any(e => e.LearnerId == learnerId))
                {
                    status = "Enrolled";
                }
                else
                {
                    var orderedWaitlist = state.Waitlist
                        .OrderBy(w => w.SequenceNumber)
                        .ToList();

                    var index = orderedWaitlist.FindIndex(
                        w => w.LearnerId == learnerId);

                    if (index >= 0)
                    {
                        status = "Waitlisted";
                        position = index + 1;
                    }
                }

                return new LearnerStatusResult
                {
                    LearnerId = learnerId,
                    Status = status,
                    IsEligible = learner.HasMetPrerequisite,
                    WaitlistPosition = position
                };
            });
        }


    }
}


