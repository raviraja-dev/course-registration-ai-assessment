# Course Registration – AI-Assisted Engineering Assessment

## Overview

A focused ASP.NET Core Web API implementation of course enrollment, dropping, and FIFO waitlist promotion for a single course offering.

The project demonstrates C# business logic, a service layer, repository abstraction, dependency injection, LINQ, in-memory concurrency control, and automated testing.

The implementation is intentionally limited to the course-registration workflow described in the assessment.

## Technology Stack

- C#
- ASP.NET Core Web API (.NET 8)
- In-memory storage
- LINQ
- Dependency Injection
- Repository Pattern
- xUnit

No external database or frontend is required.

## Features

### Enrollment
- Enroll eligible learners when capacity is available.
- Place eligible learners on a FIFO waitlist when the course is full.
- Prevent duplicate enrollment and waitlist entries.
- Reject learners who do not satisfy the prerequisite rule.
- Return clear results for invalid requests.

### Dropping and Waitlist Promotion
- Remove an enrolled learner.
- Automatically promote the earliest eligible waitlisted learner.
- Preserve FIFO ordering.
- Skip and retain learners who become ineligible.
- Handle invalid and repeated drop requests safely.

### Learner Status (Diagnostic Endpoint)
- Check whether a learner is enrolled, waitlisted, or not registered.
- Return the current waitlist position and prerequisite eligibility.

This optional read-only endpoint was added to support local verification and is not part of the assessment's mandatory feature scope.

## Project Structure

```text
CourseRegistration/
├── CourseRegistration.Api/
│   ├── Controllers/
│   │   └── EnrollmentController.cs
│   └── Program.cs
│
├── CourseRegistration.Application/
│   ├── Interfaces/
│   │   ├── ICourseRepository.cs
│   │   └── IEnrollmentService.cs
│   ├── Models/
│   └── Services/
│       └── EnrollmentService.cs
│
├── CourseRegistration.Domain/
│   └── Entities/
│       ├── CourseOffering.cs
│       ├── Learner.cs
│       ├── Enrollment.cs
│       └── WaitlistEntry.cs
│
├── CourseRegistration.Infrastructure/
│   ├── Data/
│   │   └── SampleData.cs
│   └── Repositories/
│       └── InMemoryCourseRepository.cs
│
├── CourseRegistration.Tests/
│   └── EnrollmentServiceTests.cs
│
├── README.md
├── DESIGN.md
├── AI_USAGE.md
└── LIMITATIONS.md
```

## Prerequisites

- .NET 8 SDK
- Visual Studio with ASP.NET and web development workload, or another compatible .NET development environment

## Setup and Run

Clone the repository:

```bash
git clone <YOUR-GITHUB-REPOSITORY-URL>
cd CourseRegistration
```

Restore and build:

```bash
dotnet restore
dotnet build
```

Run the API:

```bash
dotnet run --project CourseRegistration.Api
```

Use the localhost address displayed by the application.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/enrollments/{learnerId}` | Enroll or waitlist a learner |
| DELETE | `/api/enrollments/{learnerId}` | Drop and promote an eligible waitlisted learner |
| GET | `/api/enrollments/status/{learnerId}` | Check learner status (optional diagnostic) |

### Example Workflow

For a course with capacity 2:

1. Enroll learner 101 — enrolled.
2. Enroll learner 102 — enrolled.
3. Enroll learner 103 — waitlisted at position 1.
4. Enroll learner 104 — waitlisted at position 2.
5. Drop learner 101 — learner 103 is automatically promoted.
6. Check learner 104's status — waitlist position becomes 1.

The standalone demonstration initializes one offering with capacity 2 and sample learners 101–105. Learner 105 initially does not satisfy the prerequisite.

## Automated Tests

Run all tests:

```bash
dotnet test
```

Tests cover:

- Available-capacity enrollment
- Full-capacity waitlisting
- Duplicate enrollment and waitlist requests
- Prerequisite failures
- Invalid or unknown learners
- Successful drops and repeated drops
- FIFO promotion
- Skipping and retaining ineligible waitlisted learners
- Waitlist-position updates
- Concurrent registration for the last available seat
- Concurrent duplicate requests

The test suite was reported passing in Visual Studio during development. See `AI_USAGE.md` for verification context.

## Architecture

The implementation uses API, Application, Domain, and Infrastructure layers.

Business rules are implemented in `EnrollmentService`. In-memory repository operations use a shared lock to protect registration state during enrollment, dropping, and promotion.

For additional details, refer to `DESIGN.md`.

## AI-Assisted Engineering

AI was used for requirement interpretation, code guidance, design review, debugging assistance, and test suggestions.

The implementation was developed iteratively, with code corrections and locally executed tests.

See `AI_USAGE.md` for prompt records, reviewed suggestions, and verification evidence.

## Limitations

- Data is stored in memory and resets when the application restarts.
- Concurrency protection is limited to one application process and the shared repository lock.
- The current state model has mutable collections, so callers must follow the repository synchronization boundary.
- Prerequisite eligibility is represented by a boolean rather than an external academic-record system.
- No frontend or persistent database is implemented.

See `LIMITATIONS.md` for further details.
