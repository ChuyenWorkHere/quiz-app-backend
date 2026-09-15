using QuizApplication.Common;
using QuizApplication.DTOs;

namespace QuizApplication.Services
{
    public interface IQuizAttemptService
    {
        Task<PagedResult<AttemptDto>> GetAttemptsAsyncByUserId(string userId, int page, int pageSize);

    }
}
