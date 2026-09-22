using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Enums;

namespace QuizApplication.Services
{
    public interface IAdminUserService
    {
        Task<AdminUserListResultDto> GetUsersAsync(int page, int pageSize, string? search, string? role, UserStatus? status);
        Task<AdminUserDetailDto> GetUserAsync(string id);
        Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequestDto request);
        Task<AdminUserDto> UpdateStatusAsync(string id, string currentAdminId, UpdateUserStatusRequestDto request);
        Task<AdminUserDto> UpdateRoleAsync(string id, string currentAdminId, UpdateUserRoleRequestDto request);
    }
}
