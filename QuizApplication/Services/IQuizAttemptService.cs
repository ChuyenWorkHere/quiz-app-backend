using QuizApplication.Common;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;

namespace QuizApplication.Services
{
    public interface IQuizAttemptService
    {
        Task<AttemptHistoryResultDto> GetAttemptsAsyncByUserId(
            string userId, int page, int pageSize, string? search = null, bool? isPassed = null);
        Task<QuizAttemptSessionDto> StartAttemptAsync(int quizId, string userId);
        Task<QuizAttemptSessionDto> GetAttemptSessionAsync(int attemptId, string userId);
        Task<QuizAttemptResultDto> SubmitAttemptAsync(int attemptId, string userId, SubmitAttemptRequestDto request);
        Task<QuizAttemptResultDto> GetAttemptResultAsync(int attemptId, string userId);
        Task<QuizAttemptReviewDto> GetAttemptReviewAsync(int attemptId, string userId);
    }
}
