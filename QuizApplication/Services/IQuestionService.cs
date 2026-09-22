using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Enums;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        Task<PagedResult<QuestionDto>> GetAllQuestionsAsync(
            int page,
            int pageSize,
            string? search = null,
            QuestionType? questionType = null,
            QuestionAssignmentStatus assignmentStatus = QuestionAssignmentStatus.ALL);

        Task<PagedResult<QuestionDto>> GetUnassignedQuestionsAsync(int page, int pageSize);
        Task<PagedResult<QuestionDto>> GetQuestionsByActiveQuizIdAsync(int quizId, int page, int pageSize);
        Task<PagedResult<QuestionDto>> GetQuestionsByQuizIdAsync(int quizId, int page, int pageSize);
        Task<QuestionDto?> GetQuestionByIdAsync(int id);
        Task<QuestionDto> AddQuestionAsync(QuestionRequestDto request);
        Task<List<QuestionDto>> AddQuestionsAsync(List<QuestionRequestDto> requests);
        Task<QuestionDto?> UpdateQuestionAsync(int id, QuestionRequestDto request);
        Task<bool> DeleteQuestionAsync(int id);

    }
}
