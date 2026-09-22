using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs
{
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public int Attempts { get; set; }
        public double AverageScore { get; set; }
    }

    public class AdminUserListResultDto : QuizApplication.Common.PagedResult<AdminUserDto>
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int AdminUsers { get; set; }
    }

    public class AdminUserDetailDto : AdminUserDto
    {
        public List<AdminUserAttemptDto> RecentAttempts { get; set; } = new();
    }

    public class AdminUserAttemptDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public double Score { get; set; }
        public bool IsPassed { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
