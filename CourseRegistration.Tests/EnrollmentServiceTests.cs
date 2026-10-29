
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using CourseRegistration.Domain.Entities;
using CourseRegistration.Application.Models;
using CourseRegistration.Application.Services;
using CourseRegistration.Infrastructure.Repositories;

namespace CourseRegistration.Tests
{
    public class EnrollmentServiceTests
    {
        private static (
            EnrollmentService Service,
            CourseRegistrationState State,
            InMemoryCourseRepository Repository)
            CreateTestContext(int capacity, params int[] learnerIds)
        {
            var course = new CourseOffering(1, capacity);
            var state = new CourseRegistrationState(course);

            foreach (var learnerId in learnerIds)
            {
                state.Learners.Add(
                    learnerId,
                    new Learner(learnerId, true));
            }

            var repository = new InMemoryCourseRepository(state);
            var service = new EnrollmentService(repository);

            return (service, state, repository);
        }

        // TEST 1: Enrollment when seats are available
        [Fact]
        public void Enroll_WhenCapacityAvailable_ShouldEnrollLearner()
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            var result = service.Enroll(101);

            Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
            Assert.Single(state.Enrollments);
            Assert.Equal(101, state.Enrollments[0].LearnerId);
            Assert.Empty(state.Waitlist);
        }

        // TEST 2: Waitlist when course is full
        [Fact]
        public void Enroll_WhenCourseFull_ShouldAddLearnerToWaitlist()
        {
            var (service, state, _) =
                CreateTestContext(2, 101, 102, 103);

            service.Enroll(101);
            service.Enroll(102);

            var result = service.Enroll(103);

            Assert.Equal(EnrollmentStatus.Waitlisted, result.Status);
            Assert.Equal(1, result.WaitlistPosition);
            Assert.Equal(2, state.Enrollments.Count);
            Assert.Single(state.Waitlist);
            Assert.Equal(103, state.Waitlist[0].LearnerId);
            Assert.Equal(2, state.NextSequenceNumber);
        }

        // TEST 3: Duplicate enrollment
        [Fact]
        public void Enroll_WhenAlreadyEnrolled_ShouldNotCreateDuplicate()
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            var firstResult = service.Enroll(101);
            var secondResult = service.Enroll(101);

            Assert.Equal(EnrollmentStatus.Enrolled, firstResult.Status);
            Assert.Equal(
                EnrollmentStatus.AlreadyEnrolled,
                secondResult.Status);

            Assert.Single(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 4: Duplicate waitlist request
        [Fact]
        public void Enroll_WhenAlreadyWaitlisted_ShouldNotCreateDuplicate()
        {
            var (service, state, _) =
                CreateTestContext(1, 101, 102);

            service.Enroll(101);
            var firstResult = service.Enroll(102);
            var secondResult = service.Enroll(102);

            Assert.Equal(EnrollmentStatus.Waitlisted, firstResult.Status);
            Assert.Equal(
                EnrollmentStatus.AlreadyWaitlisted,
                secondResult.Status);

            Assert.Single(state.Waitlist);
            Assert.Single(state.Enrollments);
            Assert.Equal(2, state.NextSequenceNumber);
        }

        // TEST 5: Prerequisite failure
        [Fact]
        public void Enroll_WhenPrerequisiteNotMet_ShouldRejectLearner()
        {
            var (service, state, repository) =
                CreateTestContext(2, 101);

            repository.ExecuteAtomically(s =>
            {
                s.Learners[101] = new Learner(101, false);
                return true;
            });

            var result = service.Enroll(101);

            Assert.Equal(
                EnrollmentStatus.PrerequisiteNotMet,
                result.Status);

            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 6: Invalid learner ID
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Enroll_WhenLearnerIdInvalid_ShouldReject(int learnerId)
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            var result = service.Enroll(learnerId);

            Assert.Equal(
                EnrollmentStatus.InvalidLearner,
                result.Status);

            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 7: Learner not found
        [Fact]
        public void Enroll_WhenLearnerDoesNotExist_ShouldReject()
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            var result = service.Enroll(999);

            Assert.Equal(
                EnrollmentStatus.InvalidLearner,
                result.Status);

            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 8: Drop without a waitlist
        [Fact]
        public void Drop_WhenNoWaitlist_ShouldReleaseSeat()
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            service.Enroll(101);

            var result = service.Drop(101);

            Assert.Equal(DropStatus.Dropped, result.Status);
            Assert.Null(result.PromotedLearnerId);
            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 9: FIFO promotion and queue advancement
        [Fact]
        public void Drop_WhenWaitlistExists_ShouldPromoteEarliestLearner()
        {
            var (service, state, _) =
                CreateTestContext(2, 101, 102, 103, 104);

            service.Enroll(101);
            service.Enroll(102);
            service.Enroll(103);
            service.Enroll(104);

            var result = service.Drop(101);

            Assert.Equal(DropStatus.Dropped, result.Status);
            Assert.Equal(103, result.PromotedLearnerId);

            Assert.Equal(2, state.Enrollments.Count);
            Assert.DoesNotContain(
                state.Enrollments,
                e => e.LearnerId == 101);

            Assert.Contains(
                state.Enrollments,
                e => e.LearnerId == 103);

            Assert.Single(state.Waitlist);
            Assert.Equal(104, state.Waitlist[0].LearnerId);
            Assert.Equal(2L, state.Waitlist[0].SequenceNumber);
        }

        // TEST 10: Skip and retain an ineligible learner
        [Fact]
        public void Drop_WhenFirstWaitlistedLearnerIneligible_ShouldPromoteNextEligible()
        {
            var (service, state, repository) =
                CreateTestContext(1, 101, 102, 103);

            service.Enroll(101);
            service.Enroll(102);
            service.Enroll(103);

            // Simulate eligibility changing after waitlisting.
            repository.ExecuteAtomically(s =>
            {
                s.Learners[102] = new Learner(102, false);
                return true;
            });

            var result = service.Drop(101);

            Assert.Equal(DropStatus.Dropped, result.Status);
            Assert.Equal(103, result.PromotedLearnerId);

            Assert.Single(state.Enrollments);
            Assert.Equal(103, state.Enrollments[0].LearnerId);

            Assert.Single(state.Waitlist);
            Assert.Equal(102, state.Waitlist[0].LearnerId);
            Assert.Equal(1L, state.Waitlist[0].SequenceNumber);
        }

        // TEST 11: No eligible learner for promotion
        [Fact]
        public void Drop_WhenNoEligibleWaitlistedLearner_ShouldLeaveSeatVacant()
        {
            var (service, state, repository) =
                CreateTestContext(1, 101, 102);

            service.Enroll(101);
            service.Enroll(102);

            repository.ExecuteAtomically(s =>
            {
                s.Learners[102] = new Learner(102, false);
                return true;
            });

            var result = service.Drop(101);

            Assert.Equal(DropStatus.Dropped, result.Status);
            Assert.Null(result.PromotedLearnerId);

            Assert.Empty(state.Enrollments);
            Assert.Single(state.Waitlist);
            Assert.Equal(102, state.Waitlist[0].LearnerId);
        }

        // TEST 12: Repeated drop
        [Fact]
        public void Drop_WhenAlreadyDropped_ShouldReturnNotEnrolled()
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            service.Enroll(101);

            var firstResult = service.Drop(101);
            var secondResult = service.Drop(101);

            Assert.Equal(DropStatus.Dropped, firstResult.Status);
            Assert.Equal(
                DropStatus.NotEnrolled,
                secondResult.Status);

            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        // TEST 13: Invalid drop IDs
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Drop_WhenLearnerIdInvalid_ShouldReject(int learnerId)
        {
            var (service, state, _) =
                CreateTestContext(2, 101);

            service.Enroll(101);

            var result = service.Drop(learnerId);

            Assert.Equal(DropStatus.InvalidLearner, result.Status);
            Assert.Single(state.Enrollments);
        }

        // TEST 14: Drop of learner who is not enrolled
        [Fact]
        public void Drop_WhenLearnerNotEnrolled_ShouldNotChangeState()
        {
            var (service, state, _) =
                CreateTestContext(1, 101, 102);

            service.Enroll(101);

            var result = service.Drop(102);

            Assert.Equal(DropStatus.NotEnrolled, result.Status);
            Assert.Single(state.Enrollments);
            Assert.Equal(101, state.Enrollments[0].LearnerId);
        }

        // TEST 15: Concurrent requests for the last seat
        [Fact]
        public async Task Enroll_WhenConcurrentRequests_ShouldNotExceedCapacity()
        {
            var (service, state, repository) =
                CreateTestContext(1, 101, 102);

            using var start = new ManualResetEventSlim(false);
            using var ready = new CountdownEvent(2);

            Task<EnrollmentResult> EnrollConcurrently(int id)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        ready.Signal();
                        start.Wait();
                        return service.Enroll(id);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            var task1 = EnrollConcurrently(101);
            var task2 = EnrollConcurrently(102);

            try
            {
                Assert.True(
                    ready.Wait(TimeSpan.FromSeconds(10)),
                    "Workers did not become ready.");

                start.Set();

                var results = await Task.WhenAll(task1, task2);

                Assert.Single(
                    results.Where(r => r.Status == EnrollmentStatus.Enrolled));

                Assert.Single(
                    results.Where(r => r.Status == EnrollmentStatus.Waitlisted));

                repository.ExecuteAtomically(s =>
                {
                    Assert.Single(s.Enrollments);
                    Assert.Single(s.Waitlist);

                    Assert.NotEqual(
                        s.Enrollments[0].LearnerId,
                        s.Waitlist[0].LearnerId);

                    Assert.True(
                        s.Enrollments.Count <= s.CourseOffering.Capacity);

                    return true;
                });
            }
            finally
            {
                start.Set();
            }
        }

        // TEST 16: Concurrent duplicate request
        [Fact]
        public async Task Enroll_WhenSameLearnerRegistersConcurrently_ShouldNotDuplicate()
        {
            var (service, state, repository) =
                CreateTestContext(1, 101);

            using var start = new ManualResetEventSlim(false);
            using var ready = new CountdownEvent(2);

            Task<EnrollmentResult> EnrollConcurrently()
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        ready.Signal();
                        start.Wait();
                        return service.Enroll(101);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            var task1 = EnrollConcurrently();
            var task2 = EnrollConcurrently();

            try
            {
                Assert.True(ready.Wait(TimeSpan.FromSeconds(10)));

                start.Set();

                var results = await Task.WhenAll(task1, task2);

                Assert.Single(
                    results.Where(r => r.Status == EnrollmentStatus.Enrolled));

                Assert.Single(
                    results.Where(r => r.Status == EnrollmentStatus.AlreadyEnrolled));

                repository.ExecuteAtomically(s =>
                {
                    Assert.Single(s.Enrollments);
                    Assert.Empty(s.Waitlist);
                    return true;
                });
            }
            finally
            {
                start.Set();
            }
        }


        [Fact]
        public void GetLearnerStatus_WhenEnrolled_ShouldReturnEnrolled()
        {
            // Arrange
            var (service, state, _) = CreateTestContext(2, 101);

            service.Enroll(101);

            // Act
            var result = service.GetLearnerStatus(101);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(101, result.LearnerId);
            Assert.Equal("Enrolled", result.Status);
            Assert.True(result.IsEligible);
            Assert.Null(result.WaitlistPosition);
        }


        [Fact]
        public void GetLearnerStatus_WhenWaitlisted_ShouldReturnPosition()
        {
            // Arrange
            var (service, state, _) =
                CreateTestContext(2, 101, 102, 103, 104);

            service.Enroll(101);
            service.Enroll(102);
            service.Enroll(103);
            service.Enroll(104);

            // Act
            var result103 = service.GetLearnerStatus(103);
            var result104 = service.GetLearnerStatus(104);

            // Assert
            Assert.NotNull(result103);
            Assert.Equal("Waitlisted", result103.Status);
            Assert.Equal(1, result103.WaitlistPosition);
            Assert.True(result103.IsEligible);

            Assert.NotNull(result104);
            Assert.Equal("Waitlisted", result104.Status);
            Assert.Equal(2, result104.WaitlistPosition);
            Assert.True(result104.IsEligible);
        }

        [Fact]
        public void GetLearnerStatus_AfterPromotion_ShouldUpdatePositions()
        {
            // Arrange
            var (service, state, _) =
                CreateTestContext(2, 101, 102, 103, 104);

            service.Enroll(101);
            service.Enroll(102);
            service.Enroll(103);
            service.Enroll(104);

            // Act
            var dropResult = service.Drop(101);

            var learner103 = service.GetLearnerStatus(103);
            var learner104 = service.GetLearnerStatus(104);

            // Assert
            Assert.Equal(103, dropResult.PromotedLearnerId);

            Assert.NotNull(learner103);
            Assert.Equal("Enrolled", learner103.Status);
            Assert.Null(learner103.WaitlistPosition);

            Assert.NotNull(learner104);
            Assert.Equal("Waitlisted", learner104.Status);
            Assert.Equal(1, learner104.WaitlistPosition);

            Assert.Equal(2, state.Enrollments.Count);
            Assert.Single(state.Waitlist);
        }

        [Fact]
        public void GetLearnerStatus_WhenWaitlistedLearnerBecomesIneligible_ShouldShowEligibility()
        {
            // Arrange
            var (service, state, repository) =
                CreateTestContext(1, 101, 102, 103);

            service.Enroll(101);
            service.Enroll(102);
            service.Enroll(103);

            // Simulate prerequisite eligibility changing
            repository.ExecuteAtomically(s =>
            {
                s.Learners[102] = new Learner(102, false);
                return true;
            });

            // Act
            var learner102 = service.GetLearnerStatus(102);
            var dropResult = service.Drop(101);
            var learner102AfterDrop = service.GetLearnerStatus(102);
            var learner103 = service.GetLearnerStatus(103);

            // Assert
            Assert.NotNull(learner102);
            Assert.Equal("Waitlisted", learner102.Status);
            Assert.False(learner102.IsEligible);
            Assert.Equal(1, learner102.WaitlistPosition);

            Assert.Equal(103, dropResult.PromotedLearnerId);

            Assert.NotNull(learner102AfterDrop);
            Assert.Equal("Waitlisted", learner102AfterDrop.Status);
            Assert.False(learner102AfterDrop.IsEligible);
            Assert.Equal(1, learner102AfterDrop.WaitlistPosition);

            Assert.NotNull(learner103);
            Assert.Equal("Enrolled", learner103.Status);
        }

        [Fact]
        public void GetLearnerStatus_WhenNotRegistered_ShouldReturnNotRegistered()
        {
            // Arrange
            var (service, state, _) =
                CreateTestContext(2, 101);

            // Act
            var result = service.GetLearnerStatus(101);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NotRegistered", result.Status);
            Assert.True(result.IsEligible);
            Assert.Null(result.WaitlistPosition);

            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(999)]
        public void GetLearnerStatus_WhenLearnerInvalidOrUnknown_ShouldReturnNull(
            int learnerId)
        {
            // Arrange
            var (service, state, _) =
                CreateTestContext(2, 101);

            // Act
            var result = service.GetLearnerStatus(learnerId);

            // Assert
            Assert.Null(result);
            Assert.Empty(state.Enrollments);
            Assert.Empty(state.Waitlist);
        }

        [Fact]
        public void GetLearnerStatus_WhenPrerequisiteNotMet_ShouldReturnNotRegisteredAndIneligible()
        {
            // Arrange
            var (service, state, repository) =
                CreateTestContext(2, 105);

            repository.ExecuteAtomically(s =>
            {
                s.Learners[105] = new Learner(105, false);
                return true;
            });

            // Act
            var result = service.GetLearnerStatus(105);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NotRegistered", result.Status);
            Assert.False(result.IsEligible);
            Assert.Null(result.WaitlistPosition);
        }

    }
}
