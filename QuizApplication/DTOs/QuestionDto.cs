using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Image { get; set; }
        public QuestionLevel Level { get; set; }
        public QuestionType QuestionType { get; set; }
        public int? QuizId { get; set; }
        public string QuizTitle { get; set; }

        public List<AnswerDto> Answers { get; set; }
    }
}
