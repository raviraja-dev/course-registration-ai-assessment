# AI-Assisted Engineering – Collaboration and Prompt Record

## 1. Overview

During the Course Registration and Waitlist assessment, I used ChatGPT as an AI engineering assistant to discuss architectural choices, review implementation decisions, resolve coding issues, explore edge cases, and prepare technical documentation.

My approach was iterative rather than generating the complete application in a single prompt. I developed the solution in Visual Studio, discussed individual design and implementation challenges with AI, evaluated its suggestions, and made changes based on the assessment requirements.

The collaboration involved decisions in both directions: I accepted some AI recommendations, requested changes to others, and introduced additional considerations that led to revised solutions.

## 2. AI Tool and Collaboration Approach

**AI Tool:** ChatGPT (OpenAI)

**Development Environment:** Visual Studio, ASP.NET Core Web API, C#, xUnit

**Working approach:**

1. Understand the assessment and its boundaries.
2. Discuss a suitable architecture.
3. Implement domain entities and application components incrementally.
4. Review code and resolve issues through AI-assisted discussions.
5. Discuss edge cases and concurrency behavior.
6. Execute tests locally and review the results.
7. Use AI to help structure and refine technical documentation.

AI served as an engineering assistant and review partner. Architectural choices and implementation adjustments were evaluated against the assessment requirements.

## 3. Prompt and Decision History

The following records contain representative prompts from my development conversation. They are excerpts rather than a complete transcript.

### Discussion 1 – Choosing the Architecture

**My prompt:**

> "Or u suggest me the better apparch is that apilayer, service layer and database layer would be best or any other idea would be the best idea"

**Discussion:**

I started by considering an API layer, service layer, and data-access layer.

ChatGPT suggested separating Domain, Application, Infrastructure, and API responsibilities while keeping the implementation lightweight.

We discussed the assessment's restriction against unnecessary database infrastructure and chose an in-memory repository.

**Decision:** Accepted the layered architecture with in-memory persistence.

**Implementation influence:**
- CourseRegistration.Api
- CourseRegistration.Application
- CourseRegistration.Domain
- CourseRegistration.Infrastructure
- CourseRegistration.Tests

**Reason:** Maintain separation of concerns, dependency injection, and testability without adding features outside the assessment.

### Discussion 2 – Optimistic vs. Pessimistic Concurrency

**My prompt:**

> "why cant we use concurrency here if two user register for one single seat avaialable optimestic and pessimestic apporach is there nah"

**Discussion:**

I raised the scenario where two learners simultaneously request the last available seat.

We compared optimistic and pessimistic concurrency approaches.

ChatGPT explained how optimistic concurrency could use version checking in a database-backed implementation and how a C# lock could serialize in-memory operations.

**Decision:** Selected pessimistic synchronization using `lock` for the in-memory prototype.

**Implementation:**
- ICourseRepository
- InMemoryCourseRepository
- ExecuteAtomically

**Verification:** Concurrent enrollment scenarios were included in the xUnit suite. I reported that all tests passed in Visual Studio.

### Discussion 3 – Repository Interface and Delegate Design

**Initial implementation:**

```csharp
Task<TResult> ExecuteAtomically<TResult>(
    Func<TResult> func);
```

**Discussion:**

During implementation, I needed clarification on generic delegates and how the application service should access registration state.

ChatGPT suggested a synchronous state-aware delegate because the repository performs in-memory operations under a lock.

**Revised contract:**

```csharp
TResult ExecuteAtomically<TResult>(
    Func<CourseRegistrationState, TResult> operation);
```

**Decision:** Accepted the revised signature.

**Reason:** Allow the service to perform the capacity check and state update within one synchronized operation.

### Discussion 4 – Incremental Enrollment Implementation

**My prompt:**

> "u dont directly tell what to write code i will write u review it and correct me if im wrong"

**Discussion:**

I chose to implement individual domain entities and service methods incrementally and request reviews when necessary.

During enrollment implementation, we reviewed:

- Input validation
- Learner lookup using `TryGetValue`
- Duplicate enrollment checking using LINQ `Any`
- Duplicate waitlist prevention
- Prerequisite validation
- Capacity checking
- FIFO sequence generation

AI feedback helped identify issues such as validating the wrong variable, using an incorrect return type, and placing code after a return statement.

**Decision:** Accepted relevant corrections and updated the implementation.

**Affected files:**
- CourseOffering.cs
- Learner.cs
- Enrollment.cs
- WaitlistEntry.cs
- EnrollmentService.cs

**Verification:** The resulting enrollment and validation behavior was covered by automated tests.

### Discussion 5 – FIFO Promotion and Waitlist Positions

**My prompt:**

> "wait if 2 users are enrolled say a,b then cand d are in waiting list a opted out and c gets in position of a then d is moved ahead that type of functionality we are showing like train waitlist"

**Discussion:**

I clarified that I wanted FIFO promotion to behave similarly to a train waiting list.

When an enrolled learner drops, the earliest eligible waiting learner should be promoted, and the remaining learners' current positions should move forward.

ChatGPT confirmed the approach and clarified the difference between a permanent FIFO sequence number and a current waitlist position.

**Decision:** Retain original sequence numbers and calculate current positions from the remaining queue.

**Implementation influence:**
- WaitlistEntry.SequenceNumber
- FIFO promotion logic in EnrollmentService
- Waitlist-position calculations

**Verification:** FIFO promotion and queue-position behavior were included in the automated test suite.

### Discussion 6 – Completing Drop and Promotion Logic

**My prompt:**

> "help me with Drop and promotion logic suggest me snippet of code"

**Discussion:**

After implementing the drop validation and enrollment lookup, I requested help completing the promotion logic.

ChatGPT provided an implementation that removes the enrolled learner, selects the earliest eligible waitlist entry, and promotes that learner within the same synchronized operation.

I incorporated the suggested logic into my service and continued with verification.

**Decision:** Accepted the suggested promotion logic after reviewing its behavior against the agreed FIFO policy.

**Affected file:** EnrollmentService.cs

**Verification:** The automated tests covered successful promotion, repeated drops, no eligible learner, and skipping ineligible waitlisted learners.

### Discussion 7 – Learner Status Verification

**My prompt:**

> "i will pass learnerid he checks his position , he checks his waitinglist whtehr he is eligible or not"

**Discussion:**

I suggested a way to verify registration and promotion results by supplying an individual learner ID.

We designed a read-only endpoint returning:

- Enrollment or waitlist status
- Current waitlist position
- Prerequisite eligibility

ChatGPT assisted with the result model and service-query logic.

**Decision:** Included the learner-status endpoint as an optional development-time diagnostic.

**Affected files:**
- LearnerStatusResult.cs
- IEnrollmentService.cs
- EnrollmentService.cs
- EnrollmentController.cs

**Scope consideration:** The GET endpoint is not part of the assessment's mandatory feature scope.

### Discussion 9 – Technical Documentation

**My prompt:**

> "help me prepare README.md,DESIGN.md and the AI prompt i have used"

**Discussion:**

After implementing the application and running the automated tests, I requested assistance preparing the documentation required by the assessment.

ChatGPT helped organize the README, design specification, AI prompt record, and limitations summary.

I provided the implementation context and requested that the documentation reflect the actual architectural choices, prerequisite assumptions, FIFO promotion policy, concurrency approach, and verification activities.

**Decision:** Used AI assistance to draft and refine documentation based on the implemented solution.

**Documents influenced:**
- README.md
- DESIGN.md
- AI_USAGE.md
- LIMITATIONS.md

**Verification:** Documentation should be checked against the final source code, project structure, and test results before submission.

## 4. Summary of Engineering Decisions

| Topic | My contribution / decision | AI contribution | Outcome |
|---|---|---|---|
| Technology | Chose ASP.NET Core Web API and C# | Discussed suitable architecture | .NET implementation |
| Architecture | Proposed API, service, and data layers | Suggested domain and infrastructure separation | Lightweight layered design |
| Concurrency | Raised simultaneous registration and locking strategies | Compared approaches and suggested `lock` | Synchronized repository operations |
| Domain modeling | Implemented entity classes incrementally | Reviewed validation and encapsulation | Simple domain entities |
| Enrollment | Implemented validation, capacity checking, and waitlisting step by step | Identified mistakes and suggested corrections | Enrollment workflow |
| FIFO | Clarified train-style queue advancement | Explained sequence and eligibility handling | FIFO promotion |
| Drop | Implemented initial validation and lookup | Supplied completion of promotion logic | Drop workflow |
| Controller | Requested simpler logic | Supplied `if`-based alternative | Simplified controller |
| Learner status | Proposed checking individual learner status | Assisted with model and LINQ query | Diagnostic GET endpoint |
| Documentation | Requested assessment-specific documents and refinements | Helped draft and structure them | Technical documentation |

## 5. Independent Verification

The following activities were performed during development:

- Created the .NET solution in Visual Studio.
- Implemented and revised the application components.
- Built the complete solution successfully.
- Integrated the service and repository logic.
- Executed the xUnit test suite locally.
- Confirmed that all tests passed.

**Test execution command:**

```bash
dotnet test
```

**Verification status:** All tests passed in my Visual Studio run.

The exact test count and execution log can be included from the final test run as supporting evidence.

## 6. Limitations and Review Considerations

We discussed the limitations of the in-memory approach.

The shared repository lock protects operations executed through that repository instance, but it does not provide cross-process synchronization.

The registration-state collections are mutable, so access outside the synchronized repository operation remains a design limitation.

We considered stronger state encapsulation but deferred a larger refactor to keep the implementation focused on the assessment's bounded requirements.

## 7. Conclusion

This assessment was developed through an iterative AI-assisted engineering workflow.

ChatGPT acted as a technical discussion partner, assisted with code reviews, helped resolve implementation issues, suggested improvements, and supported preparation of technical documentation.

I made and refined design decisions throughout development, including the technology stack, concurrency approach, FIFO behavior, controller simplicity, and learner-status verification.

The resulting implementation was verified through locally executed automated tests rather than relying solely on AI suggestions.
