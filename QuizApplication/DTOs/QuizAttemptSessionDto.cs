using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class QuizAttemptSessionDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int Duration { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public AttemptStatus Status { get; set; }
        public List<AttemptQuestionDto> Questions { get; set; } = new();
    }

    public class AttemptQuestionDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public List<AttemptAnswerOptionDto> Answers { get; set; } = new();
    }

    public class AttemptAnswerOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
