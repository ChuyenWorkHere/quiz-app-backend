namespace QuizApplication.Models
{
    public class UserAnswer
    {
        public int Id { get; set; }
        public int QuizAttemptId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }
        public QuizAttempt QuizAttempt { get; set; }
        public Question Question { get; set; }
        public Answer Answer { get; set; }
    }
}
