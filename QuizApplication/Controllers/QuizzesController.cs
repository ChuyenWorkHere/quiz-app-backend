using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Constants;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{

    [Route("api/quizzes")]
    [ApiController]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizzesController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveQuizzes([FromQuery] int page, [FromQuery] int pageSize)
        {
            PagedResult<QuizDto> responses = await _quizService.GetActiveQuizzesAsync(page, pageSize);
            return Ok(responses);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetActiveQuizById([FromRoute] int id)
        {
            QuizDto response = await _quizService.GetActiveQuizByIdAsync(id);
            return Ok(response);
        }     
    }
}
