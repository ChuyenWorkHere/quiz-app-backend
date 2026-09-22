namespace QuizApplication.DTOs
{
    public class QuizAttemptResultDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsPassed { get; set; }
        public int PassedScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DurationSeconds { get; set; }
        public List<AttemptQuestionResultDto> QuestionResults { get; set; } = new();
    }

    public class AttemptQuestionResultDto
    {
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }
    }
}
