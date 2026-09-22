using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Services;
using QuizApplication.DTOs.Requests;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    [Authorize]
    [Route("api/attempts")]
    [ApiController]
    public class AttemptsController : ControllerBase
    {
        private readonly IQuizAttemptService _quizAttemptService;

        public AttemptsController(IQuizAttemptService quizAttemptService)
        {
            _quizAttemptService = quizAttemptService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAttempts([FromRoute] string userId, 
            [FromQuery] int page, [FromQuery] int pageSize,
            [FromQuery] string? search, [FromQuery] bool? isPassed)
        {
            var userLoggedInId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != userLoggedInId)
            {
                throw new UnauthorizedAccessException("Can't access this data");
            }

            var pagedResult = await _quizAttemptService.GetAttemptsAsyncByUserId(
                userId, page, pageSize, search, isPassed);
            return Ok(pagedResult);
        }

        [HttpPost("/api/quizzes/{quizId:int}/attempts")]
        public async Task<IActionResult> StartAttempt([FromRoute] int quizId)
        {
            var userId = GetCurrentUserId();
            return Ok(await _quizAttemptService.StartAttemptAsync(quizId, userId));
        }

        [HttpGet("{attemptId:int}/session")]
        public async Task<IActionResult> GetAttemptSession([FromRoute] int attemptId)
        {
            var userId = GetCurrentUserId();
            return Ok(await _quizAttemptService.GetAttemptSessionAsync(attemptId, userId));
        }

        [HttpPost("{attemptId:int}/submit")]
        public async Task<IActionResult> SubmitAttempt(
            [FromRoute] int attemptId,
            [FromBody] SubmitAttemptRequestDto request)
        {
            var userId = GetCurrentUserId();
            return Ok(await _quizAttemptService.SubmitAttemptAsync(attemptId, userId, request));
        }

        [HttpGet("{attemptId:int}/result")]
        public async Task<IActionResult> GetAttemptResult([FromRoute] int attemptId)
        {
            var userId = GetCurrentUserId();
            return Ok(await _quizAttemptService.GetAttemptResultAsync(attemptId, userId));
        }

        [HttpGet("{attemptId:int}/review")]
        public async Task<IActionResult> GetAttemptReview([FromRoute] int attemptId)
        {
            var userId = GetCurrentUserId();
            return Ok(await _quizAttemptService.GetAttemptReviewAsync(attemptId, userId));
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Can't access this data");
        }
    }
}
