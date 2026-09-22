using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class AttemptDto
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double Score { get; set; }
        public bool IsPassed { get; set; }
        public AttemptStatus Status { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int DurationSeconds { get; set; }
        public QuizDto Quiz { get; set; } = null!;
    }

    public class AttemptHistoryResultDto : QuizApplication.Common.PagedResult<AttemptDto>
    {
        public int TotalCompleted { get; set; }
        public double AverageScore { get; set; }
        public int PassedAttempts { get; set; }
        public int TotalDurationSeconds { get; set; }
        public double BestScore { get; set; }
        public string? BestQuizTitle { get; set; }
    }
}
