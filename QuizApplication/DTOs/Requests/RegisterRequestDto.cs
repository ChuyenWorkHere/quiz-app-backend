using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.Requests
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Fullname is required!")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Username is required!")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required!")]
        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Password is required!")]
        public string Password { get; set; }
    }
}
