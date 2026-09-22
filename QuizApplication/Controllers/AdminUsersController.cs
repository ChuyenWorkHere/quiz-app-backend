using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApplication.DTOs.Requests;
using QuizApplication.Models.Constants;
using QuizApplication.Models.Enums;
using QuizApplication.Services;
using System.Security.Claims;

namespace QuizApplication.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = Roles.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] UserStatus? status = null)
        {
            return Ok(await _adminUserService.GetUsersAsync(page, pageSize, search, role, status));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser([FromRoute] string id)
        {
            return Ok(await _adminUserService.GetUserAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequestDto request)
        {
            var response = await _adminUserService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = response.Id }, response);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            [FromRoute] string id, [FromBody] UpdateUserStatusRequestDto request)
        {
            return Ok(await _adminUserService.UpdateStatusAsync(id, CurrentUserId(), request));
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(
            [FromRoute] string id, [FromBody] UpdateUserRoleRequestDto request)
        {
            return Ok(await _adminUserService.UpdateRoleAsync(id, CurrentUserId(), request));
        }

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is missing");
    }
}
