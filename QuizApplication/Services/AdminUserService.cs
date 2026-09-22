using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.Exceptions;
using QuizApplication.Models;
using QuizApplication.Models.Constants;
using QuizApplication.Models.Enums;

namespace QuizApplication.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly QuizDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminUserService(QuizDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<AdminUserListResultDto> GetUsersAsync(
            int page, int pageSize, string? search, string? role, UserStatus? status)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var allUsers = _context.Users.AsNoTracking();
            var totalUsers = await allUsers.CountAsync();
            var activeUsers = await allUsers.CountAsync(user => user.IsActive == UserStatus.ACTIVE);
            var lockedUsers = totalUsers - activeUsers;
            var adminRoleId = await _context.Roles.Where(item => item.Name == Roles.Admin)
                .Select(item => item.Id).FirstOrDefaultAsync();
            var adminUsers = adminRoleId == null ? 0 : await _context.UserRoles.CountAsync(item => item.RoleId == adminRoleId);

            IQueryable<User> query = allUsers;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(user => user.FullName.Contains(value)
                    || (user.UserName != null && user.UserName.Contains(value))
                    || (user.Email != null && user.Email.Contains(value)));
            }
            if (status.HasValue)
                query = query.Where(user => user.IsActive == status.Value);
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleId = await _context.Roles.Where(item => item.Name == role)
                    .Select(item => item.Id).FirstOrDefaultAsync();
                query = roleId == null
                    ? query.Where(_ => false)
                    : query.Where(user => _context.UserRoles.Any(item => item.UserId == user.Id && item.RoleId == roleId));
            }

            var totalItems = await query.CountAsync();
            var users = await query.OrderByDescending(user => user.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var items = new List<AdminUserDto>();
            foreach (var user in users) items.Add(await MapUserAsync(user));

            return new AdminUserListResultDto
            {
                Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                TotalUsers = totalUsers, ActiveUsers = activeUsers,
                LockedUsers = lockedUsers, AdminUsers = adminUsers
            };
        }

        public async Task<AdminUserDetailDto> GetUserAsync(string id)
        {
            var user = await FindUserAsync(id);
            var dto = await MapUserAsync(user);
            return new AdminUserDetailDto
            {
                Id = dto.Id, FullName = dto.FullName, Username = dto.Username, Email = dto.Email,
                Phone = dto.Phone, DateOfBirth = dto.DateOfBirth, Avatar = dto.Avatar,
                Status = dto.Status, CreatedAt = dto.CreatedAt, UpdatedAt = dto.UpdatedAt,
                Roles = dto.Roles, Attempts = dto.Attempts, AverageScore = dto.AverageScore,
                RecentAttempts = await _context.QuizAttempts.AsNoTracking()
                    .Where(attempt => attempt.UserId == id && attempt.Status == AttemptStatus.Submitted)
                    .OrderByDescending(attempt => attempt.SubmittedAt).Take(3)
                    .Select(attempt => new AdminUserAttemptDto
                    {
                        AttemptId = attempt.Id, QuizId = attempt.QuizId, QuizTitle = attempt.Quiz.Title,
                        Score = attempt.Score, IsPassed = attempt.IsPassed, SubmittedAt = attempt.SubmittedAt
                    }).ToListAsync()
            };
        }

        public async Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequestDto request)
        {
            ValidateRole(request.Role);
            if (await _userManager.FindByEmailAsync(request.Email.Trim()) != null)
                throw new BadRequestException("Email is already in use");
            if (await _userManager.FindByNameAsync(request.Username.Trim()) != null)
                throw new BadRequestException("Username is already in use");

            var user = new User
            {
                FullName = request.FullName.Trim(), UserName = request.Username.Trim(),
                Email = request.Email.Trim(), IsActive = UserStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));
            await _userManager.AddToRoleAsync(user, request.Role);
            return await MapUserAsync(user);
        }

        public async Task<AdminUserDto> UpdateStatusAsync(
            string id, string currentAdminId, UpdateUserStatusRequestDto request)
        {
            if (id == currentAdminId && request.Status == UserStatus.LOCKED)
                throw new BadRequestException("You cannot lock your own account");
            var user = await FindUserAsync(id);
            user.IsActive = request.Status;
            user.UpdatedAt = DateTime.UtcNow;
            await SaveUserAsync(user);
            return await MapUserAsync(user);
        }

        public async Task<AdminUserDto> UpdateRoleAsync(
            string id, string currentAdminId, UpdateUserRoleRequestDto request)
        {
            ValidateRole(request.Role);
            if (id == currentAdminId && !request.Role.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("You cannot remove your own administrator role");
            var user = await FindUserAsync(id);
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0) await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await _userManager.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));
            user.UpdatedAt = DateTime.UtcNow;
            await SaveUserAsync(user);
            return await MapUserAsync(user);
        }

        private async Task<User> FindUserAsync(string id) =>
            await _userManager.FindByIdAsync(id) ?? throw new NotFoundException("User not found");

        private static void ValidateRole(string role)
        {
            if (role != Roles.Admin && role != Roles.User)
                throw new BadRequestException("Role must be Admin or User");
        }

        private async Task SaveUserAsync(User user)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        private async Task<AdminUserDto> MapUserAsync(User user)
        {
            var completed = _context.QuizAttempts.Where(attempt => attempt.UserId == user.Id
                && attempt.Status != AttemptStatus.InProgress);
            var attemptCount = await completed.CountAsync();
            return new AdminUserDto
            {
                Id = user.Id, FullName = user.FullName, Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty, Phone = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth, Avatar = user.Avatar, Status = user.IsActive,
                CreatedAt = user.CreatedAt, UpdatedAt = user.UpdatedAt,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(), Attempts = attemptCount,
                AverageScore = attemptCount == 0 ? 0 : Math.Round(await completed.AverageAsync(attempt => attempt.Score), 2)
            };
        }
    }
}
