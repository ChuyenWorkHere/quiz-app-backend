using Microsoft.AspNetCore.Identity;
using QuizApplication.Models.Enums;

namespace QuizApplication.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Avatar { get; set; }

        public UserStatus IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Quiz> CreatedQuizzes { get; set; }
        public ICollection<QuizAttempt> QuizAttempts { get; set; }
    }
}
