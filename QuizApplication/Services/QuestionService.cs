using Microsoft.EntityFrameworkCore;
using QuizApplication.Common;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models;
using QuizApplication.Exceptions;
using QuizApplication.Models.Enums;

namespace QuizApplication.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly QuizDbContext _context;
        private readonly IQuizService _quizService;

        public QuestionService(QuizDbContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        public async Task<QuestionDto> AddQuestionAsync(QuestionRequestDto request)
        {
            Question question = await BuildQuestionAsync(request);
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return MapToQuestionDto(question);
        }

        public async Task<List<QuestionDto>> AddQuestionsAsync(List<QuestionRequestDto> requests)
        {
            if (requests == null || requests.Count == 0)
                throw new BadRequestException("At least one question is required");

            var questions = new List<Question>(requests.Count);
            for (var index = 0; index < requests.Count; index++)
            {
                try
                {
                    questions.Add(await BuildQuestionAsync(requests[index]));
                }
                catch (Exception exception) when (exception is BadRequestException or NotFoundException)
                {
                    throw new BadRequestException($"Question at index {index} is invalid: {exception.Message}");
                }
            }

            _context.Questions.AddRange(questions);
            await _context.SaveChangesAsync();
            return questions.Select(MapToQuestionDto).ToList();
        }

        private async Task<Question> BuildQuestionAsync(QuestionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Question content is required");

            Quiz? quiz = null;
            if (request.QuizId.HasValue)
            {
                quiz = await _context.Quizzes.FindAsync(request.QuizId.Value);
                if (quiz == null) throw new NotFoundException("Quiz not found");
            }

            var answers = request.Answers
                .Where(answer => !string.IsNullOrWhiteSpace(answer.Text))
                .ToList();

            if (answers.Count < 2) 
                throw new BadRequestException("A question must have at least two answers");

            var correctAnswerCount = answers.Count(answer => answer.IsCorrect);
            if (request.QuestionType == QuestionType.SINGLE_CHOICE && correctAnswerCount != 1)
                throw new BadRequestException("A single-choice question must have exactly one correct answer");
            if (request.QuestionType == QuestionType.MULTIPLE_SELECT && correctAnswerCount < 1)
                throw new BadRequestException("A multiple-select question must have at least one correct answer");
            if (request.QuestionType == QuestionType.TRUE_FALSE && (answers.Count != 2 || correctAnswerCount != 1))
                throw new BadRequestException("A true/false question must have two answers and exactly one correct answer");

            return new Question
            {
                Content = request.Content.Trim(),
                Image = request.Image?.Trim() ?? string.Empty,
                Level = request.Level,
                QuestionType = request.QuestionType,
                QuizId = request.QuizId,
                Quiz = quiz,
                Answers = answers.Select(answer => new Answer
                {
                    Text = answer.Text.Trim(),
                    IsCorrect = answer.IsCorrect
                }).ToList()
            };
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            Question? question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                throw new NotFoundException("Question not found");

            bool hasStudentAnswers = await _context.UserAnswers
                .AnyAsync(userAnswer => userAnswer.QuestionId == id);
            if (hasStudentAnswers)
                throw new BadRequestException(
                    "This question cannot be deleted because it already has student answers");

            if (question.QuizId.HasValue && question.Quiz?.IsActive == true)
            {
                int questionCount = await _context.Questions
                    .CountAsync(item => item.QuizId == question.QuizId);
                if (questionCount <= 1)
                    throw new BadRequestException(
                        "This question cannot be deleted because it is the last question in an active quiz");
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<QuestionDto>> GetAllQuestionsAsync(
            int page,
            int pageSize,
            string? search = null,
            QuestionType? questionType = null,
            QuestionAssignmentStatus assignmentStatus = QuestionAssignmentStatus.ALL)
        {
            if (page <= 0 || pageSize <= 0)
                throw new BadRequestException("Page size and page number must be greater than 0");

            IQueryable<Question> query = _context.Questions
                .AsNoTracking()
                .Include(q => q.Answers)
                .Include(q => q.Quiz);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchTerm = search.Trim();
                bool isQuestionId = int.TryParse(searchTerm, out int questionId);
                query = query.Where(q =>
                    q.Content.Contains(searchTerm) ||
                    (q.Quiz != null && q.Quiz.Title.Contains(searchTerm)) ||
                    (isQuestionId && q.Id == questionId));
            }

            if (questionType.HasValue)
                query = query.Where(q => q.QuestionType == questionType.Value);

            if (assignmentStatus == QuestionAssignmentStatus.ASSIGNED)
                query = query.Where(q => q.QuizId.HasValue);
            else if (assignmentStatus == QuestionAssignmentStatus.UNASSIGNED)
                query = query.Where(q => !q.QuizId.HasValue);

            var totalQuestions = await query.CountAsync();
            var questionEntities = await query
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalQuestions / pageSize);

            var questionResponses = questionEntities.Select(qe => MapToQuestionDto(qe)).ToList();
            PagedResult<QuestionDto> result = new PagedResult<QuestionDto>
            {
                Items = questionResponses,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalQuestions,
            };

            return result;
        }


        public async Task<QuestionDto?> GetQuestionByIdAsync(int id)
        {
            Question? question = await _context.Questions
                .AsNoTracking()
                .Include(q => q.Answers)
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.Id == id);
            return question == null ? null : MapToQuestionDto(question);
        }


        public async Task<QuestionDto?> UpdateQuestionAsync(int id, QuestionRequestDto request)
        {
            Question? question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return null;

            if (string.IsNullOrWhiteSpace(request.Content))
                throw new BadRequestException("Question content is required");

            Quiz? quiz = null;
            if (request.QuizId.HasValue)
            {
                quiz = await _context.Quizzes.FindAsync(request.QuizId.Value);
                if (quiz == null) throw new NotFoundException("Quiz not found");
            }

            var answers = request.Answers.Where(answer => !string.IsNullOrWhiteSpace(answer.Text)).ToList();
            if (answers.Count < 2) throw new BadRequestException("A question must have at least two answers");

            var correctAnswerCount = answers.Count(answer => answer.IsCorrect);
            if (request.QuestionType == QuestionType.SINGLE_CHOICE && correctAnswerCount != 1)
                throw new BadRequestException("A single-choice question must have exactly one correct answer");
            if (request.QuestionType == QuestionType.MULTIPLE_SELECT && correctAnswerCount < 1)
                throw new BadRequestException("A multiple-select question must have at least one correct answer");
            if (request.QuestionType == QuestionType.TRUE_FALSE && (answers.Count != 2 || correctAnswerCount != 1))
                throw new BadRequestException("A true/false question must have two answers and exactly one correct answer");

            question.Content = request.Content.Trim();
            question.Image = request.Image?.Trim() ?? string.Empty;
            question.Level = request.Level;
            question.QuestionType = request.QuestionType;
            question.QuizId = request.QuizId;
            question.Quiz = quiz;
            _context.Answers.RemoveRange(question.Answers);
            question.Answers = answers.Select(answer => new Answer
            {
                Text = answer.Text.Trim(),
                IsCorrect = answer.IsCorrect
            }).ToList();

            await _context.SaveChangesAsync();
            return MapToQuestionDto(question);
        }

        private static QuestionDto MapToQuestionDto(Question question)
        {
            return new QuestionDto()
            {
                Id = question.Id,
                Content = question.Content,
                Image = question.Image,
                Level = question.Level,
                QuestionType = question.QuestionType,
                Answers = question.Answers.Select(a => new AnswerDto()
                {
                    Id = a.Id,
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    QuestionId = a.QuestionId,
                }).ToList(),
                QuizId = question.QuizId,
                QuizTitle = question.Quiz == null ? "" : question.Quiz.Title,
            };
        }

        public async Task<PagedResult<QuestionDto>> GetUnassignedQuestionsAsync(int page, int pageSize)
        {
            var totalQuestions = await _context.Questions.CountAsync(q => q.QuizId == null);
            var questionEntities = await _context.Questions
                .AsNoTracking()
                .Where(q => q.QuizId == null)
                .Include(q => q.Answers)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalQuestions / pageSize);

            var questionResponses = questionEntities.Select(qe => MapToQuestionDto(qe)).ToList();
            PagedResult<QuestionDto> result = new PagedResult<QuestionDto>
            {
                Items = questionResponses,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalQuestions,
            };

            return result;
        }

        public async Task<PagedResult<QuestionDto>> GetQuestionsByActiveQuizIdAsync(int quizId, int page, int pageSize)
        {
            Quiz quiz = _context.Quizzes.Find(quizId);
            if (quiz == null)
            {
                throw new BadRequestException("Quiz not found");
            }
            return await GetAllQuestionsByQuizIdAsync(quizId, page, pageSize, true);
        }

        public async Task<PagedResult<QuestionDto>> GetQuestionsByQuizIdAsync(int quizId, int page, int pageSize)
        {
            Quiz quiz = _context.Quizzes.Find(quizId);
            if (quiz == null)
            {
                throw new BadRequestException("Quiz not found");
            }
            return await GetAllQuestionsByQuizIdAsync(quizId, page, pageSize, false);
        }

        private async Task<PagedResult<QuestionDto>> GetAllQuestionsByQuizIdAsync(int quizId, int page, int pageSize, bool isActive)
        {
            if (page <= 0 || pageSize <= 0)
                throw new BadRequestException("Page size and page number must be greater than 0");

            IQueryable<Question> query = _context.Questions
                    .AsNoTracking()
                    .Include(q => q.Answers)
                    .Include(q => q.Quiz)
                    .Where(q => q.QuizId == quizId);

            if (isActive)
            {
                query = query.Where(q => q.Quiz.IsActive);
            }

            var totalQuestions = await query.CountAsync();

            var questionEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = questionEntities
                .Select(MapToQuestionDto)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalQuestions / pageSize);

            PagedResult<QuestionDto> pagedResult = new PagedResult<QuestionDto>()
            {
                Items = result,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalQuestions,
                TotalPages = totalPages
            };
            return pagedResult;
        }
    }
}
