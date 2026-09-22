using Microsoft.AspNetCore.Identity;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Exceptions;
using QuizApplication.Models;

namespace QuizApplication.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<User> _userManager;

        public UserProfileService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserDto> GetProfileAsync(string userId)
        {
            var user = await FindUserAsync(userId);
            return await MapToDtoAsync(user);
        }

        public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request)
        {
            var user = await FindUserAsync(userId);
            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                throw new BadRequestException("Date of birth cannot be in the future");

            user.FullName = request.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            user.DateOfBirth = request.DateOfBirth?.Date;
            user.Avatar = string.IsNullOrWhiteSpace(request.Avatar) ? null : request.Avatar;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));

            return await MapToDtoAsync(user);
        }

        private async Task<User> FindUserAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException("User not found");
        }

        private async Task<UserDto> MapToDtoAsync(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Avatar = user.Avatar,
                Status = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };
        }
    }
}
