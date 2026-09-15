namespace QuizApplication.DTOs.Responses
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
        public DateTime ExpriresAt { get; set; }
        public UserDto User { get; set; }
    }
}
