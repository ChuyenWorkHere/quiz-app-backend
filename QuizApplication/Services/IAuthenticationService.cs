using QuizApplication.DTOs.Requests;
using QuizApplication.DTOs.Responses;

namespace QuizApplication.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponseDto> RegisterAsync(RegisterRequestDto requestDto);

        Task<LoginResponseDto> LoginAsync(LoginRequestDto requestDto);
    }
}
