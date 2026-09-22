using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;

namespace QuizApplication.Services
{
    public interface IUserProfileService
    {
        Task<UserDto> GetProfileAsync(string userId);
        Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request);
    }
}
