using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;

namespace QuizApplication.Services
{
    public interface IQuizService
    {
        Task<PagedResult<QuizDto>> GetQuizzesAsync(
            int page,
            int pageSize,
            string? search = null,
            bool? isActive = null,
            string sortBy = "recent");

        Task<PagedResult<QuizDto>> GetActiveQuizzesAsync(int page, int pageSize);

        Task<QuizDto> GetQuizByIdAsync(int id);

        Task<QuizDto> GetActiveQuizByIdAsync(int id);

        Task<QuizDto> AddQuiz(QuizRequestDto quiz, string addedByUserId);

        Task<QuizDto> UpdateQuiz(int id, QuizRequestDto quiz);

        Task<bool> DeleteQuiz(int id);

        Task<int> DeleteQuizzes(IReadOnlyCollection<int> ids);
    }
}
