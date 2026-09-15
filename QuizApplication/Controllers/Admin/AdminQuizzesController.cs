using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Constants;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers.Admin
{
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin/quizzes")]
    [ApiController]
    public class AdminQuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public AdminQuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuizzes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string sortBy = "recent")
        {
            PagedResult<QuizDto> responses = await _quizService.GetQuizzesAsync(
                page, pageSize, search, isActive, sortBy);
            return Ok(responses);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetQuizById([FromRoute] int id)
        {
            QuizDto response = await _quizService.GetQuizByIdAsync(id);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuiz([FromBody] QuizRequestDto quizRequest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            QuizDto response = await _quizService.AddQuiz(quizRequest, userId!);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuiz([FromRoute] int id, [FromBody] QuizRequestDto quizRequest)
        {
            QuizDto response = await _quizService.UpdateQuiz(id, quizRequest);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuiz([FromRoute] int id)
        {
            await _quizService.DeleteQuiz(id);
            return NoContent();
        }

        [HttpPost("bulk-delete")]
        public async Task<IActionResult> DeleteQuizzes([FromBody] List<int> ids)
        {
            int deletedCount = await _quizService.DeleteQuizzes(ids);
            return Ok(new { deletedCount });
        }
    }
}
