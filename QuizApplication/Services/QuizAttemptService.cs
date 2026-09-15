using Microsoft.EntityFrameworkCore;
using QuizApplication.Common;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.Models;

namespace QuizApplication.Services
{
    public class QuizAttemptService : IQuizAttemptService
    {
        private readonly QuizDbContext _context;


        public QuizAttemptService(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AttemptDto>> GetAttemptsAsyncByUserId(string userId, int page, int pageSize)
        {
            var totalAttempts = await _context.QuizAttempts
                .Where(at => at.UserId == userId)
                .CountAsync();

            var attemptsQuery = _context.QuizAttempts
                .Where(at => at.UserId == userId)
                .Select(at => MapToAttempDto(at));

            var attempts = await attemptsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            PagedResult<AttemptDto> pagedResult = new PagedResult<AttemptDto>
            {
                Items = attempts,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalAttempts,
                TotalPages = (int)Math.Ceiling((double)totalAttempts / pageSize)
            };

            return pagedResult;
        }

        private static AttemptDto MapToAttempDto(QuizAttempt quizAttempt)
        {
            return new AttemptDto
            {
                Id = quizAttempt.Id,
                Score = quizAttempt.Score,
                SubmittedAt = quizAttempt.SubmittedAt,
                Quiz = new QuizDto
                {
                    Id = quizAttempt.Quiz.Id,
                    Title = quizAttempt.Quiz.Title,
                    Description = quizAttempt.Quiz.Description,
                    CreatedAt = quizAttempt.Quiz.CreatedAt,
                    UpdatedAt = quizAttempt.Quiz.UpdatedAt,
                    Image = quizAttempt.Quiz.Image,
                    Duration = quizAttempt.Quiz.Duration,
                    IsActive = quizAttempt.Quiz.IsActive,
                    PassedScore = quizAttempt.Quiz.PassedScore,
                    NumberOfQuestions = quizAttempt.Quiz.Questions.Count(),
                }
            };
        }
    }
}
