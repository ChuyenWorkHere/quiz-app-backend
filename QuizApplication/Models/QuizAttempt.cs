using QuizApplication.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizApplication.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int QuizId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double Score { get; set; }

        public bool IsPassed { get; set; }

        public AttemptStatus Status { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public Quiz Quiz { get; set; }
        public ICollection<UserAnswer> UserAnswers { get; set; }
    }
}
