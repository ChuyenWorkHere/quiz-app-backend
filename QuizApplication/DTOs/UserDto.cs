using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }

        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public List<string> Roles { get; set; }
    }
}
