using QuizApplication.Models.Enums;

namespace QuizApplication.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Image { get; set; }
        public QuestionLevel Level { get; set; }
        public QuestionType QuestionType { get; set; }
        public int? QuizId { get; set; }
        public Quiz? Quiz { get; set; }
        public ICollection<Answer> Answers { get; set; }
        public ICollection<UserAnswer> UserAnswers { get; set; }
    }
}
