using Microsoft.EntityFrameworkCore;
using QuizApplication.Common;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Exceptions;
using QuizApplication.Models;
using System.Security.Claims;


namespace QuizApplication.Services
{
    public class QuizService : IQuizService
    {
        private readonly QuizDbContext _context;

        public QuizService(QuizDbContext context)
        {
            _context = context;
        }

        public async Task<QuizDto> AddQuiz(QuizRequestDto quiz, string addedByUserId)
        {
            List<int> questionIds = quiz.QuestionIds.Distinct().ToList();
            ValidateActiveQuizHasQuestions(quiz.IsActive, questionIds);

            List<Question> selectedQuestions = await _context.Questions
                .Where(question => questionIds.Contains(question.Id))
                .ToListAsync();
            ValidateSelectedQuestions(questionIds, selectedQuestions, null);

            Quiz newQuiz = new Quiz()
            {
                Title = quiz.Title,
                Description = quiz.Description,
                Duration = quiz.Duration,
                Image = quiz.Image,
                PassedScore = quiz.PassedScore,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = quiz.IsActive,
                UserId = addedByUserId,
                Questions = selectedQuestions
            };
            
            await _context.AddAsync(newQuiz);
            await _context.SaveChangesAsync();

            return MapToQuizDto(newQuiz);
        }

        public async Task<bool> DeleteQuiz(int id)
        {
            Quiz? quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quiz == null) throw new NotFoundException("Quiz not found");

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(attempt => attempt.QuizId == id);
            if (hasAttempts)
                throw new BadRequestException("This quiz cannot be deleted because it already has student attempts");

            foreach (Question question in quiz.Questions)
            {
                question.QuizId = null;
                question.Quiz = null;
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteQuizzes(IReadOnlyCollection<int> ids)
        {
            List<int> quizIds = ids.Distinct().ToList();
            if (quizIds.Count == 0)
                throw new BadRequestException("At least one quiz ID is required");

            List<Quiz> quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => quizIds.Contains(q.Id))
                .ToListAsync();
            if (quizzes.Count != quizIds.Count)
                throw new NotFoundException("One or more quizzes were not found");

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(attempt => quizIds.Contains(attempt.QuizId));
            if (hasAttempts)
                throw new BadRequestException("The selected quizzes cannot be deleted because at least one already has student attempts");

            foreach (Question question in quizzes.SelectMany(quiz => quiz.Questions))
            {
                question.QuizId = null;
                question.Quiz = null;
            }

            _context.Quizzes.RemoveRange(quizzes);
            await _context.SaveChangesAsync();
            return quizzes.Count;
        }

        public async Task<QuizDto> GetQuizByIdAsync(int id)
        {
            Quiz? quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }

            //Update attempts & passed percentage
            return MapToQuizDto(quiz);
        }

        public async Task<QuizDto> GetActiveQuizByIdAsync(int id)
        {
            Quiz? quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (quiz == null)
            {
                throw new NotFoundException("Active quiz not found");
            }

            return MapToQuizDto(quiz);
        }

        public async Task<PagedResult<QuizDto>> GetQuizzesAsync(
            int page,
            int pageSize,
            string? search = null,
            bool? isActive = null,
            string sortBy = "recent")
        {
            return await GetPagedQuizzesAsync(page, pageSize, activeOnly: false, search, isActive, sortBy);
        }

        public async Task<PagedResult<QuizDto>> GetActiveQuizzesAsync(int page, int pageSize)
        {
            return await GetPagedQuizzesAsync(page, pageSize, activeOnly: true);
        }

        private async Task<PagedResult<QuizDto>> GetPagedQuizzesAsync(
            int page,
            int pageSize,
            bool activeOnly,
            string? search = null,
            bool? isActive = null,
            string sortBy = "recent")
        {
            if (pageSize <= 0 || page <= 0)
            {
                throw new BadRequestException("Page size and page number must be greater than 0");
            }
            var query = _context.Quizzes
                .AsNoTracking();

            if(activeOnly)
            {
                query = query.Where(q => q.IsActive);
            }

            if (isActive.HasValue)
            {
                query = query.Where(q => q.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(q =>
                    q.Title.Contains(keyword) || q.Description.Contains(keyword));
            }

            query = sortBy.Trim().ToLowerInvariant() switch
            {
                "title" => query.OrderBy(q => q.Title),
                "questions" => query.OrderByDescending(q => q.Questions.Count),
                "duration" => query.OrderByDescending(q => q.Duration),
                "recent" => query.OrderByDescending(q => q.UpdatedAt),
                _ => throw new BadRequestException("Sort by must be recent, title, questions, or duration")
            };

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize
            );

            //Update attempts & passed percentage
            List<Quiz> quizEntities = await query
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<QuizDto> quizzes = quizEntities
                .Select(MapToQuizDto)
                .ToList();

            return new PagedResult<QuizDto>
            {
                Items = quizzes,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<QuizDto> UpdateQuiz(int id, QuizRequestDto quiz)
        {
            Quiz? existQuiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (existQuiz == null)
            {
                throw new NotFoundException("Quiz not found");
            }
            existQuiz.Title = quiz.Title;
            existQuiz.Description = quiz.Description;
            existQuiz.Duration = quiz.Duration;
            existQuiz.Image = quiz.Image;
            existQuiz.PassedScore = quiz.PassedScore;
            existQuiz.IsActive = quiz.IsActive;
            existQuiz.UpdatedAt = DateTime.Now;

            List<int> questionIds = quiz.QuestionIds.Distinct().ToList();
            ValidateActiveQuizHasQuestions(quiz.IsActive, questionIds);
            List<Question> selectedQuestions = await _context.Questions
                .Where(question => questionIds.Contains(question.Id))
                .ToListAsync();
            ValidateSelectedQuestions(questionIds, selectedQuestions, id);

            foreach (Question question in existQuiz.Questions
                         .Where(question => !questionIds.Contains(question.Id)).ToList())
            {
                question.QuizId = null;
                question.Quiz = null;
            }

            foreach (Question question in selectedQuestions)
            {
                question.QuizId = id;
                question.Quiz = existQuiz;
            }

            await _context.SaveChangesAsync();

            return MapToQuizDto(existQuiz);
        }

        private static void ValidateSelectedQuestions(
            IReadOnlyCollection<int> requestedIds,
            IReadOnlyCollection<Question> selectedQuestions,
            int? currentQuizId)
        {
            if (selectedQuestions.Count != requestedIds.Count)
                throw new BadRequestException("One or more selected questions do not exist");

            if (selectedQuestions.Any(question =>
                    question.QuizId.HasValue && question.QuizId != currentQuizId))
                throw new BadRequestException("One or more selected questions already belong to another quiz");
        }

        private static void ValidateActiveQuizHasQuestions(bool isActive, IReadOnlyCollection<int> questionIds)
        {
            if (isActive && questionIds.Count == 0)
                throw new BadRequestException("An active quiz must contain at least one question");
        }

        private static QuizDto MapToQuizDto(Quiz quiz)
        {
            var attempts = quiz.QuizAttempts.Count();
            var passedPercentage = attempts == 0 ? 0
                : quiz.QuizAttempts.Count(a => a.Score > quiz.PassedScore);

            return new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                Duration = quiz.Duration,
                Image = quiz.Image,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt,
                PassedScore = quiz.PassedScore,
                IsActive = quiz.IsActive,
                Attempts = attempts,
                NumberOfQuestions = quiz.Questions.Count(),
                PassRate = passedPercentage,
                
            };
        }
    }
}
