using QuizApplication.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizApplication.DTOs
{
    public class AttemptDto
    {
        public int Id { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double Score { get; set; }
        public UserDto User { get; set; }
        public QuizDto Quiz { get; set; }
    }
}
