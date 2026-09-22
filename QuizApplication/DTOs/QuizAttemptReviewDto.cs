using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class QuizAttemptReviewDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsPassed { get; set; }
        public int PassedScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int DurationSeconds { get; set; }
        public List<AttemptQuestionReviewDto> Questions { get; set; } = new();
    }

    public class AttemptQuestionReviewDto
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
        public List<AttemptAnswerReviewDto> Answers { get; set; } = new();
    }

    public class AttemptAnswerReviewDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsCorrect { get; set; }
    }
}
