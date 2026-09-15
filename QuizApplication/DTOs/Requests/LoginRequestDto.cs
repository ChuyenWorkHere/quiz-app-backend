using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.Requests
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is requiređ")]
        public string Password { get; set; }
    }
}
