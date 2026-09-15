using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.Requests
{
    public class AnswerRequestDto
    {
        [Required, MinLength(1)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
