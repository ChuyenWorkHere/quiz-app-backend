namespace QuizApplication.DTOs.Requests
{
    public class QuizRequestDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public string Image { get; set; }

        public int PassedScore { get; set; }

        public bool IsActive { get; set; }

        public List<int> QuestionIds { get; set; } = new();
    }
}
