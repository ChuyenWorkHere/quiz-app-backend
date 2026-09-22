using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Constants;
using QuizApplication.Models.Enums;
using QuizApplication.Services;

namespace QuizApplication.Controllers.Admin
{
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin/questions")]
    [ApiController]
    public class AdminQuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public AdminQuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<QuestionDto>>> GetQuestions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] QuestionType? questionType = null,
            [FromQuery] QuestionAssignmentStatus assignmentStatus = QuestionAssignmentStatus.ALL)
        {
            PagedResult<QuestionDto> response =
                await _questionService.GetAllQuestionsAsync(page, pageSize, search, questionType, assignmentStatus);

            return Ok(response);
        }

        [HttpGet("unassigned")]
        public async Task<ActionResult<PagedResult<QuestionDto>>> GetUnassignedQuestions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            PagedResult<QuestionDto> response =
                await _questionService.GetUnassignedQuestionsAsync(page, pageSize);

            return Ok(response);
        }

        [HttpGet("/api/admin/quizzes/{quizId:int}/questions")]
        public async Task<ActionResult<PagedResult<QuestionDto>>> GetQuestionsByQuizId(
            [FromRoute] int quizId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            return Ok(await _questionService.GetQuestionsByQuizIdAsync(quizId, page, pageSize));
        }

        [HttpPost]
        public async Task<ActionResult<QuestionDto>> AddQuestion([FromBody] QuestionRequestDto request)
        {
            QuestionDto response = await _questionService.AddQuestionAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<List<QuestionDto>>> AddQuestions(
            [FromBody] List<QuestionRequestDto> requests)
        {
            List<QuestionDto> response = await _questionService.AddQuestionsAsync(requests);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<QuestionDto>> GetQuestionById([FromRoute] int id)
        {
            QuestionDto? response = await _questionService.GetQuestionByIdAsync(id);
            if (response == null) throw new QuizApplication.Exceptions.NotFoundException("Question not found");
            return Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<QuestionDto>> UpdateQuestion([FromRoute] int id, [FromBody] QuestionRequestDto request)
        {
            QuestionDto? response = await _questionService.UpdateQuestionAsync(id, request);
            if (response == null) throw new QuizApplication.Exceptions.NotFoundException("Question not found");
            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteQuestion([FromRoute] int id)
        {
            await _questionService.DeleteQuestionAsync(id);
            return NoContent();
        }
    }
}
