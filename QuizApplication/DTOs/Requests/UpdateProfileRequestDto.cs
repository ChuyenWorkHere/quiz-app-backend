using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.Requests
{
    public class UpdateProfileRequestDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Url]
        public string? Avatar { get; set; }
    }
}
