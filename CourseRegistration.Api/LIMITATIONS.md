# Limitations and Follow-up Risks

## 1. Overview

This project implements a focused course enrollment and FIFO waitlist feature using ASP.NET Core Web API, C#, and an in-memory repository.

The implementation follows the assessment's scope, prioritizing correct business logic, concurrency handling, and automated testing over additional infrastructure.

The following limitations and trade-offs were identified during development.

## 2. In-Memory Data Storage

Course, learner, enrollment, and waitlist information is maintained in memory.

- Data is lost when the application restarts.
- No persistent database is used.
- Initial demonstration data is loaded when the application starts.

**Reason:** The assessment permits an in-memory implementation when no starter project or database is provided.

## 3. Concurrency Limitations

The application uses a C# `lock` through `InMemoryCourseRepository` to synchronize enrollment, dropping, and waitlist promotion.

- Operations using the same repository instance are synchronized.
- The locking mechanism works within a single application process.
- It does not provide synchronization across multiple application instances.
- Publicly mutable registration-state collections could bypass synchronization if accessed directly outside repository operations.

**Trade-off:** A shared in-memory lock is sufficient for the intended single-process prototype when all state modifications follow the repository access pattern.

## 4. Simplified Prerequisite Validation

Prerequisite eligibility is represented by the `HasMetPrerequisite` boolean property.

- No external academic-record verification is implemented.
- Eligibility is checked during enrollment and waitlist promotion.
- Eligibility changes can be simulated using in-memory test data.

**Reason:** The assessment requires a clearly documented prerequisite rule, not a complete prerequisite-management system.

## 5. Waitlist Eligibility Policy

The implementation follows a **skip-and-retain policy**.

If a waitlisted learner becomes ineligible:

- The learner is not promoted.
- Their existing waitlist entry is retained.
- The earliest eligible learner is selected for promotion.
- If no eligible learner is available, the seat remains vacant.

Sequence numbers remain unchanged when learners are promoted. Current waitlist positions are determined from the remaining waitlist entries.

This policy is intentionally fixed rather than configurable.

## 6. Single Course Offering

The application supports one course offering.

It does not implement a course catalog, multiple offerings, or multi-term registration.

**Reason:** These capabilities are outside the assessment's defined scope.

## 7. Testing Limitations

Automated xUnit tests cover enrollment, waitlisting, duplicates, prerequisites, dropping, FIFO promotion, invalid requests, and concurrency scenarios.

All tests passed during local Visual Studio execution.

However, automated tests cannot guarantee the absence of every possible race condition or unexpected runtime scenario.

## 8. Follow-up Considerations

For a production implementation, possible improvements would include:

- Persistent storage with appropriate database transactions and constraints.
- Stronger encapsulation of mutable registration state.
- Concurrency protection across multiple application instances.
- Integration with an authoritative prerequisite-verification source.

These improvements were deliberately not implemented because they would extend beyond the assessment's bounded scope.

## 9. Conclusion

The identified limitations are primarily related to in-memory persistence, single-process synchronization, and simplified prerequisite handling.

The implementation remains focused on the assessment's required enrollment, dropping, and FIFO waitlist-promotion behavior, with the known trade-offs documented.
