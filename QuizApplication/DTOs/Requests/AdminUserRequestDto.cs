using System.ComponentModel.DataAnnotations;
using QuizApplication.Models.Enums;

namespace QuizApplication.DTOs.Requests
{
    public class CreateAdminUserRequestDto
    {
        [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
        [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
        [Required] public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserStatusRequestDto
    {
        public UserStatus Status { get; set; }
    }

    public class UpdateUserRoleRequestDto
    {
        [Required] public string Role { get; set; } = string.Empty;
    }
}
