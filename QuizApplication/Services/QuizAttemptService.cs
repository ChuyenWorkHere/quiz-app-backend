using Microsoft.EntityFrameworkCore;
using QuizApplication.Common;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Exceptions;
using QuizApplication.Models;
using QuizApplication.Models.Enums;

namespace QuizApplication.Services
{
    public class QuizAttemptService : IQuizAttemptService
    {
        private readonly QuizDbContext _context;

        public QuizAttemptService(QuizDbContext context) => _context = context;

        public async Task<AttemptHistoryResultDto> GetAttemptsAsyncByUserId(
            string userId, int page, int pageSize, string? search = null, bool? isPassed = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var now = DateTime.UtcNow;

            var staleAttempts = await _context.QuizAttempts
                .Where(attempt => attempt.UserId == userId
                    && attempt.Status == AttemptStatus.InProgress
                    && attempt.ExpiresAt <= now)
                .ToListAsync();
            foreach (var staleAttempt in staleAttempts)
            {
                staleAttempt.Status = AttemptStatus.Expired;
                staleAttempt.SubmittedAt = staleAttempt.ExpiresAt;
            }
            if (staleAttempts.Count > 0)
                await _context.SaveChangesAsync();

            var completedQuery = _context.QuizAttempts
                .AsNoTracking()
                .Where(attempt => attempt.UserId == userId
                    && attempt.Status != AttemptStatus.InProgress);

            var totalCompleted = await completedQuery.CountAsync();
            var averageScore = totalCompleted == 0
                ? 0
                : await completedQuery.AverageAsync(attempt => attempt.Score);

            var passedAttempts = await completedQuery.CountAsync(attempt => attempt.IsPassed);
            var bestAttempt = await completedQuery
                .OrderByDescending(attempt => attempt.Score)
                .ThenByDescending(attempt => attempt.SubmittedAt)
                .Select(attempt => new { attempt.Score, attempt.Quiz.Title })
                .FirstOrDefaultAsync();

            var durationValues = await completedQuery
                .Select(attempt => new { attempt.StartedAt, attempt.SubmittedAt })
                .ToListAsync();

            var totalDurationSeconds = durationValues.Sum(value =>
                Math.Max(0, (int)(value.SubmittedAt - value.StartedAt).TotalSeconds));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                completedQuery = completedQuery
                    .Where(attempt => attempt.Quiz.Title.Contains(normalizedSearch));
            }
            if (isPassed.HasValue)
                completedQuery = completedQuery.Where(attempt => attempt.IsPassed == isPassed.Value);

            var totalAttempts = await completedQuery.CountAsync();
            var attemptEntities = await completedQuery
                .Include(attempt => attempt.UserAnswers)
                .Include(attempt => attempt.Quiz)
                    .ThenInclude(quiz => quiz.Questions)
                        .ThenInclude(question => question.Answers)
                .OrderByDescending(attempt => attempt.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new AttemptHistoryResultDto
            {
                Items = attemptEntities.Select(MapToAttempDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalAttempts,
                TotalPages = (int)Math.Ceiling((double)totalAttempts / pageSize),
                TotalCompleted = totalCompleted,
                AverageScore = Math.Round(averageScore, 2),
                PassedAttempts = passedAttempts,
                TotalDurationSeconds = totalDurationSeconds,
                BestScore = bestAttempt?.Score ?? 0,
                BestQuizTitle = bestAttempt?.Title
            };
        }

        public async Task<QuizAttemptSessionDto> StartAttemptAsync(int quizId, string userId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Questions)
                .ThenInclude(question => question.Answers)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.IsActive)
                ?? throw new NotFoundException("Quiz not found or is not available");

            if (quiz.Questions.Count == 0)
                throw new BadRequestException("This quiz does not contain any questions");


            var now = DateTime.UtcNow;

            var currentAttempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(attempt =>
                    attempt.QuizId == quizId 
                    && attempt.UserId == userId 
                    && attempt.Status == AttemptStatus.InProgress);

            if (currentAttempt != null && currentAttempt.ExpiresAt > now)
                return MapToSessionDto(currentAttempt, quiz);

            //Kết thúc 1 attempt
            if (currentAttempt != null)
            {
                currentAttempt.Status = AttemptStatus.Expired;
                currentAttempt.SubmittedAt = currentAttempt.ExpiresAt;
            }

            var attempt = new QuizAttempt
            {
                QuizId = quizId, 
                UserId = userId, 
                StartedAt = now,
                ExpiresAt = now.AddMinutes(quiz.Duration), 
                SubmittedAt = DateTime.MinValue,
                Score = 0,
                IsPassed = false, 
                Status = AttemptStatus.InProgress,
                UserAnswers = new List<UserAnswer>()
            };
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return MapToSessionDto(attempt, quiz);
        }

        public async Task<QuizAttemptSessionDto> GetAttemptSessionAsync(int attemptId, string userId)
        {
            var attempt = await LoadAttemptAsync(attemptId, userId);
            await EnsureAttemptCanBeAnswered(attempt);
            return MapToSessionDto(attempt, attempt.Quiz);
        }

        public async Task<QuizAttemptResultDto> SubmitAttemptAsync(int attemptId, string userId, SubmitAttemptRequestDto request)
        {
            var attempt = await LoadAttemptAsync(attemptId, userId);
            await EnsureAttemptCanBeAnswered(attempt, allowSubmissionGracePeriod: true);

            if (request.Answers.GroupBy(answer => answer.QuestionId).Any(group => group.Count() > 1))
                throw new BadRequestException("Each question can only be submitted once");

            var quizQuestions = attempt.Quiz.Questions.ToDictionary(question => question.Id);
            foreach (var submittedAnswer in request.Answers)
            {
                if (!quizQuestions.TryGetValue(submittedAnswer.QuestionId, out var question))
                    throw new BadRequestException("The submission contains a question that does not belong to this quiz");

                var validAnswerIds = question.Answers.Select(answer => answer.Id).ToHashSet();
                var answerIds = submittedAnswer.AnswerIds.Distinct().ToList();
                
                if (answerIds.Any(answerId => !validAnswerIds.Contains(answerId)))
                    throw new BadRequestException("The submission contains an invalid answer");

                foreach (var answerId in answerIds)
                    _context.UserAnswers.Add(new UserAnswer
                    {
                        QuizAttemptId = attempt.Id, 
                        QuestionId = question.Id, 
                        AnswerId = answerId
                    });
            }

            var selectedByQuestion = request.Answers.ToDictionary(
                answer => answer.QuestionId, answer => answer.AnswerIds.Distinct().ToHashSet());
            var questionResults = BuildQuestionResults(attempt.Quiz, selectedByQuestion);
            var correctCount = questionResults.Count(result => result.IsCorrect);

            attempt.Score = Math.Round(correctCount * 100d / attempt.Quiz.Questions.Count, 2);
            attempt.IsPassed = attempt.Score >= attempt.Quiz.PassedScore;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.Status = AttemptStatus.Submitted;
            await _context.SaveChangesAsync();
            return MapToResultDto(attempt, questionResults);
        }

        public async Task<QuizAttemptResultDto> GetAttemptResultAsync(int attemptId, string userId)
        {
            var attempt = await LoadAttemptAsync(attemptId, userId);
            if (attempt.Status == AttemptStatus.InProgress)
                throw new BadRequestException("This attempt has not been submitted");

            var selectedByQuestion = attempt.UserAnswers
                .GroupBy(answer => answer.QuestionId)
                .ToDictionary(group => group.Key, group => group.Select(answer => answer.AnswerId).ToHashSet());
            return MapToResultDto(attempt, BuildQuestionResults(attempt.Quiz, selectedByQuestion));
        }

        public async Task<QuizAttemptReviewDto> GetAttemptReviewAsync(int attemptId, string userId)
        {
            var attempt = await LoadAttemptAsync(attemptId, userId);
            if (attempt.Status == AttemptStatus.InProgress)
                throw new BadRequestException("This attempt has not been submitted");

            var selectedIds = attempt.UserAnswers.Select(answer => answer.AnswerId).ToHashSet();
            var questions = attempt.Quiz.Questions
                .OrderBy(question => question.Id)
                .Select((question, index) =>
                {
                    var correctIds = question.Answers.Where(answer => answer.IsCorrect)
                        .Select(answer => answer.Id).ToHashSet();
                    var questionSelectedIds = question.Answers.Where(answer => selectedIds.Contains(answer.Id))
                        .Select(answer => answer.Id).ToHashSet();
                    return new AttemptQuestionReviewDto
                    {
                        Id = question.Id,
                        Number = index + 1,
                        Content = question.Content,
                        Image = question.Image,
                        QuestionType = question.QuestionType,
                        IsAnswered = questionSelectedIds.Count > 0,
                        IsCorrect = correctIds.Count > 0 && correctIds.SetEquals(questionSelectedIds),
                        Answers = question.Answers.OrderBy(answer => answer.Id)
                            .Select(answer => new AttemptAnswerReviewDto
                            {
                                Id = answer.Id,
                                Text = answer.Text,
                                IsSelected = selectedIds.Contains(answer.Id),
                                IsCorrect = answer.IsCorrect
                            }).ToList()
                    };
                }).ToList();
            var submittedAt = attempt.SubmittedAt == DateTime.MinValue
                ? attempt.ExpiresAt
                : attempt.SubmittedAt;

            return new QuizAttemptReviewDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score,
                IsPassed = attempt.IsPassed,
                PassedScore = attempt.Quiz.PassedScore,
                CorrectAnswers = questions.Count(question => question.IsCorrect),
                TotalQuestions = questions.Count,
                DurationSeconds = Math.Max(0, (int)(submittedAt - attempt.StartedAt).TotalSeconds),
                Questions = questions
            };
        }

        private async Task<QuizAttempt> LoadAttemptAsync(int attemptId, string userId)
        {
            return await _context.QuizAttempts
                .Include(attempt => attempt.UserAnswers)
                .Include(attempt => attempt.Quiz)
                .ThenInclude(quiz => quiz.Questions)
                .ThenInclude(question => question.Answers)
                .FirstOrDefaultAsync(attempt => attempt.Id == attemptId && attempt.UserId == userId)
                ?? throw new NotFoundException("Quiz attempt not found");
        }

        private async Task EnsureAttemptCanBeAnswered(QuizAttempt attempt, bool allowSubmissionGracePeriod = false)
        {
            if (attempt.Status != AttemptStatus.InProgress)
                throw new BadRequestException("This attempt has already been completed");
            var deadline = allowSubmissionGracePeriod ? attempt.ExpiresAt.AddSeconds(5) : attempt.ExpiresAt;
            if (deadline > DateTime.UtcNow) return;

            attempt.Status = AttemptStatus.Expired;
            attempt.SubmittedAt = attempt.ExpiresAt;
            await _context.SaveChangesAsync();
            throw new BadRequestException("This attempt has expired");
        }

        private static QuizAttemptSessionDto MapToSessionDto(QuizAttempt attempt, Quiz quiz) => new()
        {
            AttemptId = attempt.Id, 
            QuizId = quiz.Id, 
            QuizTitle = quiz.Title, 
            Duration = quiz.Duration,
            StartedAt = AsUtc(attempt.StartedAt), 
            ExpiresAt = AsUtc(attempt.ExpiresAt), 
            Status = attempt.Status,
            Questions = quiz.Questions
            .OrderBy(question => question.Id)
            .Select(question => new AttemptQuestionDto
                {
                    Id = question.Id, 
                    Content = question.Content, 
                    Image = question.Image,
                    QuestionType = question.QuestionType,
                    Answers = question.Answers
                    .OrderBy(answer => answer.Id)
                    .Select(answer => new AttemptAnswerOptionDto
                    {
                        Id = answer.Id, Text = answer.Text
                    })
                    .ToList()
                })
            .ToList()
        };

        private static List<AttemptQuestionResultDto> BuildQuestionResults(
            Quiz quiz, IReadOnlyDictionary<int, HashSet<int>> selectedByQuestion)
        {
            return quiz.Questions
                .OrderBy(question => question.Id)
                .Select(question =>
                {
                    var correctIds = question.Answers
                                        .Where(answer => answer.IsCorrect)
                                        .Select(answer => answer.Id)
                                        .ToHashSet();
                    selectedByQuestion.TryGetValue(question.Id, out var selectedIds);
                    if (selectedIds == null)
                    {
                        selectedIds = new HashSet<int>();
                    }
                    return new AttemptQuestionResultDto
                    {
                        QuestionId = question.Id,
                        IsCorrect = correctIds.Count > 0 && correctIds.SetEquals(selectedIds)
                    };
                }).ToList();
        }

        private static QuizAttemptResultDto MapToResultDto(QuizAttempt attempt, List<AttemptQuestionResultDto> results)
        {
            var submittedAt = attempt.SubmittedAt == DateTime.MinValue ? attempt.ExpiresAt : attempt.SubmittedAt;
            return new QuizAttemptResultDto
            {
                AttemptId = attempt.Id, 
                QuizId = attempt.QuizId, 
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score, 
                IsPassed = attempt.IsPassed, 
                PassedScore = attempt.Quiz.PassedScore,
                CorrectAnswers = results.Count(result => result.IsCorrect), 
                TotalQuestions = results.Count,
                StartedAt = AsUtc(attempt.StartedAt), 
                SubmittedAt = AsUtc(submittedAt),
                DurationSeconds = Math.Max(0, (int)(submittedAt - attempt.StartedAt).TotalSeconds),
                QuestionResults = results
            };
        }

        private static AttemptDto MapToAttempDto(QuizAttempt quizAttempt)
        {
            var selectedByQuestion = quizAttempt.UserAnswers
                .GroupBy(answer => answer.QuestionId)
                .ToDictionary(group => group.Key,
                    group => group.Select(answer => answer.AnswerId).ToHashSet());
            var results = BuildQuestionResults(quizAttempt.Quiz, selectedByQuestion);
            var submittedAt = quizAttempt.SubmittedAt == DateTime.MinValue
                ? quizAttempt.ExpiresAt
                : quizAttempt.SubmittedAt;

            return new AttemptDto
            {
                Id = quizAttempt.Id,
                Score = quizAttempt.Score,
                IsPassed = quizAttempt.IsPassed,
                Status = quizAttempt.Status,
                CorrectAnswers = results.Count(result => result.IsCorrect),
                TotalQuestions = results.Count,
                StartedAt = AsUtc(quizAttempt.StartedAt),
                ExpiresAt = AsUtc(quizAttempt.ExpiresAt),
                SubmittedAt = AsUtc(submittedAt),
                DurationSeconds = Math.Max(0, (int)(submittedAt - quizAttempt.StartedAt).TotalSeconds),
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
                    NumberOfQuestions = quizAttempt.Quiz.Questions.Count
                }
            };
        }

        private static DateTime AsUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
