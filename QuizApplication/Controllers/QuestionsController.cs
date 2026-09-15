using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.Models.Constants;
using QuizApplication.Services;
using QuizApplication.DTOs.Requests;

namespace QuizApplication.Controllers
{
    
    [Route("api/quizzes/{quizId:int}/questions")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<QuestionDto>>> GetAllQuestionsByActiveQuizId(
            [FromRoute] int quizId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            PagedResult<QuestionDto> response =
                await _questionService.GetQuestionsByActiveQuizIdAsync(quizId, page, pageSize);

            return Ok(response);
        }

    }
}
