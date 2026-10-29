# Design Specification – Course Registration and FIFO Waitlist

## 1. Objective and Scope

Implement enrollment, dropping, and FIFO waitlist promotion for one course offering.

The service must preserve capacity constraints, prevent duplicate registrations, enforce a documented prerequisite rule, and handle invalid requests safely.

The implementation deliberately excludes a frontend, course catalog, payment system, identity integration, persistent database, and deployment infrastructure.

## 2. Architecture

The application follows a lightweight layered architecture.

```text
HTTP Client
    |
    v
EnrollmentController
    |
    v
IEnrollmentService
    |
    v
EnrollmentService
    |
    v
ICourseRepository
    |
    v
InMemoryCourseRepository
    |
    v
CourseRegistrationState
```

### API Layer

`EnrollmentController` handles HTTP requests, invokes the application service, and maps operation results to HTTP responses.

It does not contain enrollment, capacity, or promotion business rules.

### Application Layer

`EnrollmentService` implements:

- Learner validation
- Duplicate registration checks
- Prerequisite validation
- Enrollment capacity checks
- Waitlist insertion
- Drop and automatic FIFO promotion

`IEnrollmentService` defines the application operations.

`ICourseRepository` abstracts synchronized access to the registration state.

### Domain Layer

Contains the core entities:

- `CourseOffering`
- `Learner`
- `Enrollment`
- `WaitlistEntry`

### Infrastructure Layer

`InMemoryCourseRepository` provides synchronized in-memory state access.

`SampleData` initializes the demonstration offering and learners.

## 3. Domain and Data Model

| Entity | Properties | Responsibility |
|---|---|---|
| CourseOffering | CourseId, Capacity | Define offering and maximum seats |
| Learner | LearnerId, HasMetPrerequisite | Represent learner eligibility |
| Enrollment | LearnerId | Represent an occupied seat |
| WaitlistEntry | LearnerId, SequenceNumber | Preserve FIFO order |

`CourseRegistrationState` stores:

- One course offering
- A dictionary of learners
- A list of current enrollments
- A list of waitlist entries
- The next FIFO sequence number

The implementation uses one offering, so it does not require multi-course relationships.

## 4. Business Rules and Invariants

The following conditions must be preserved:

1. Enrollment count must never exceed course capacity.
2. A learner may have at most one enrollment.
3. A learner may have at most one waitlist entry.
4. A learner cannot be both enrolled and waitlisted.
5. Only eligible learners can be newly enrolled or waitlisted.
6. An ineligible learner cannot be promoted.
7. The earliest eligible learner must be promoted when a seat opens.
8. Invalid and repeated requests must not corrupt state.
9. FIFO sequence numbers must preserve original insertion order.

The capacity invariant is:

```text
0 <= Enrollments.Count <= CourseOffering.Capacity
```

## 5. Prerequisite Assumption

Eligibility is represented by `Learner.HasMetPrerequisite`.

- `true`: Learner satisfies the prerequisite.
- `false`: Learner does not satisfy the prerequisite.

A learner must be eligible before entering enrollment or the waitlist.

Eligibility is checked again during promotion.

No academic records service or configurable prerequisite engine is implemented.

## 6. Enrollment Algorithm

1. Validate the learner ID.
2. Verify the learner exists.
3. Check for an existing enrollment.
4. Check for an existing waitlist entry.
5. Verify the prerequisite.
6. If enrollment count is below capacity, create an enrollment.
7. Otherwise, create a waitlist entry using `NextSequenceNumber`.
8. Increment `NextSequenceNumber` for future insertions.
9. Return the enrollment or waitlist result.

All state-dependent checks and changes execute inside the repository's synchronized operation.

## 7. Drop and FIFO Promotion Algorithm

1. Validate the learner ID.
2. Find the learner's enrollment.
3. If no enrollment exists, return `NotEnrolled`.
4. Remove the enrollment.
5. Order the waitlist by `SequenceNumber`.
6. Select the earliest learner who currently satisfies the prerequisite.
7. If eligible, remove that learner from the waitlist and create an enrollment.
8. Return the drop result with the promoted learner ID, if any.

Dropping and promotion occur in one synchronized operation.

### Example

Before dropping:

```text
Capacity: 2

Enrolled: 101, 102
Waitlist:
  Position 1 -> 103
  Position 2 -> 104
```

After learner 101 drops:

```text
Enrolled: 102, 103
Waitlist:
  Position 1 -> 104
```

Sequence numbers are not decremented when learners are promoted. Current positions are derived from the remaining waitlist entries.

## 8. Ineligible Waitlist Policy

The chosen policy is **skip and retain**.

If the earliest waitlisted learner becomes ineligible:

- They are not promoted.
- Their existing waitlist entry is retained.
- The system continues searching for the earliest eligible learner.
- If nobody qualifies, the seat remains vacant.

For example, if learner 103 is ineligible and learner 104 is eligible, learner 104 is promoted while learner 103 remains waiting.

If an eligible new learner requests enrollment while a seat is vacant and all remaining waitlisted learners are currently ineligible, the new learner may enroll.

This policy is fixed for the assessment and is not configurable.

## 9. API Design

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/enrollments/{learnerId}` | Enroll or waitlist |
| DELETE | `/api/enrollments/{learnerId}` | Drop and promote |

An additional read-only diagnostic endpoint, `GET /api/enrollments/status/{learnerId}`, was implemented to inspect individual learner status during development. It is outside the mandatory feature scope.

### Enrollment Results

- Enrolled
- Waitlisted
- AlreadyEnrolled
- AlreadyWaitlisted
- PrerequisiteNotMet
- InvalidLearner

### Drop Results

- Dropped
- NotEnrolled
- InvalidLearner

Structured result objects are used to separate business outcomes from HTTP response handling.

## 10. Concurrency and Persistence

The implementation uses an in-memory repository with a C# `lock`.

```csharp
lock (_lock)
{
    return operation(_state);
}
```

The repository executes a complete synchronous operation while holding the lock.

This protects the capacity check and enrollment insertion from interleaving with other operations using the same repository instance.

Similarly, enrollment removal and waitlist promotion happen under the same lock.

The repository is registered as a singleton so API requests share the same in-memory state and synchronization object.

### Limitations of the Approach

- The lock does not coordinate multiple application processes.
- Data is not durable across restarts.
- Mutable state references must not be modified outside synchronized operations.
- There is no database transaction or optimistic-concurrency token.

These constraints are accepted for the single-process prototype allowed by the assessment.

## 11. Design Principles and Trade-offs

### Single Responsibility

Controllers handle HTTP behavior, services handle business workflows, and the repository handles state access and synchronization.

### Dependency Inversion

`EnrollmentService` depends on `ICourseRepository`, not the concrete in-memory repository.

### Dependency Injection

Dependencies are registered in `Program.cs`.

### Encapsulation

Domain entities restrict property modification through constructors and non-public setters. The shared state model remains mutable internally, which is a documented limitation.

### Trade-offs

- In-memory storage avoids adding an unnecessary database.
- A synchronous lock is simpler than optimistic-concurrency retries for this scope.
- The repository pattern separates business logic from storage details.
- A fixed prerequisite rule avoids unnecessary policy infrastructure.
- The service returns structured business results rather than relying on string comparisons.

## 12. Verification Strategy

Automated xUnit tests cover:

- Successful enrollment
- Full-capacity behavior
- Duplicate requests
- FIFO promotion
- Prerequisite rejection
- Ineligible waitlist retention
- Invalid and repeated drops
- Waitlist position changes
- Concurrent requests for one remaining seat
- Concurrent duplicate enrollment

Tests verify returned results and the underlying registration state.

The candidate reported that the test suite passed locally in Visual Studio.

## 13. Known Risks

The main remaining risks are publicly mutable in-memory state, lack of persistence, and synchronization limited to one application instance.

These are bounded limitations of the chosen prototype approach rather than reasons to introduce a new database or distributed infrastructure for this assessment.
