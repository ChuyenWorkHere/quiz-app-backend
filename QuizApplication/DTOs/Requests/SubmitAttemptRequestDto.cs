namespace QuizApplication.DTOs.Requests
{
    public class SubmitAttemptRequestDto
    {
        public List<SubmitQuestionAnswerDto> Answers { get; set; } = new();
    }

    public class SubmitQuestionAnswerDto
    {
        public int QuestionId { get; set; }
        public List<int> AnswerIds { get; set; } = new();
    }
}
