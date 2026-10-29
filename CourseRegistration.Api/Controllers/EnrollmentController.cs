using CourseRegistration.Application.Interfaces;
using CourseRegistration.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CourseRegistration.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }
        [HttpPost("{learnerId:int}")]
        public ActionResult<EnrollmentResult> Enroll(int learnerId)
        {
            var result = _enrollmentService.Enroll(learnerId);

            if (result.Status == EnrollmentStatus.Enrolled ||
                result.Status == EnrollmentStatus.Waitlisted)
            {
                return Ok(result);
            }

            if (result.Status == EnrollmentStatus.AlreadyEnrolled ||
                result.Status == EnrollmentStatus.AlreadyWaitlisted)
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }

        [HttpDelete("{learnerId:int}")]
        public IActionResult Drop(int learnerId)
        {
            var result = _enrollmentService.Drop(learnerId);

            if (result.Status == DropStatus.Dropped)
            {
                return Ok(result);
            }

            if (result.Status == DropStatus.NotEnrolled)
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        [HttpGet("status/{learnerId:int}")]
        public IActionResult GetLearnerStatus(int learnerId)
        {
            var result = _enrollmentService.GetLearnerStatus(learnerId);

            if (result == null)
            {
                return NotFound("Learner not found.");
            }

            return Ok(result);
        }
    }
}
