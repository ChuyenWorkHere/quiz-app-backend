using QuizApplication.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.Requests
{
    public class QuestionRequestDto
    {
        [Required, MinLength(3)]
        public string Content { get; set; } = string.Empty;
        public string? Image { get; set; }
        public QuestionLevel Level { get; set; }
        public QuestionType QuestionType { get; set; }
        [Range(1, int.MaxValue)]
        public int? QuizId { get; set; }
        [Required, MinLength(2)]
        public List<AnswerRequestDto> Answers { get; set; } = new();
    }
}
