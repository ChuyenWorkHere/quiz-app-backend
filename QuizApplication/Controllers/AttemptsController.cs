using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Services;
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
            [FromQuery] int page, [FromQuery] int pageSize)
        {
            var userLoggedInId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId != userLoggedInId)
            {
                throw new UnauthorizedAccessException("Can't access this data");
            }

            var pagedResult = await _quizAttemptService.GetAttemptsAsyncByUserId(userId, page, pageSize);
            return Ok(pagedResult);
        }
    }
}
