namespace QuizApplication.DTOs
{
    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public string Image { get; set; }
        public bool IsActive { get; set; }

        public int PassedScore { get; set; }
        public int Attempts { get; set; }

        public int NumberOfQuestions { get; set; }
        public int PassRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
    }
}
