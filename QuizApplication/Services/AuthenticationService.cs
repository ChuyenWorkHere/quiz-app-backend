using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.IdentityModel.Tokens;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.Requests;
using QuizApplication.DTOs.Responses;
using QuizApplication.Exceptions;
using QuizApplication.Models;
using QuizApplication.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QuizApplication.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly QuizDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthenticationService(UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            QuizDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto requestDto)
        {
            var user = await _userManager.FindByEmailAsync(requestDto.Email);
            if(user == null)
            {
                throw new NotFoundException("User not found");
            }

            if (user.IsActive == UserStatus.LOCKED)
                throw new UnauthorizedAccessException("This account is locked");

            if (user != null && !await _userManager.CheckPasswordAsync(user, requestDto.Password))
            {
                throw new BadRequestException("Username or password incorrect");
            }
            else
            {
                var response = await GenerateJwtToken(user);
                return response;
            }
            

            
        }

        public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto requestDto)
        {
            var userExists = await _userManager.FindByEmailAsync(requestDto.Email);

            if (userExists != null)
            {
                throw new BadRequestException("User already exists");
            }

            User newUser = new User
            {
                FullName = requestDto.FullName,
                PhoneNumber = requestDto.Phone,
                DateOfBirth = requestDto.DateOfBirth,
                UserName = requestDto.Username,
                Email = requestDto.Email,
                SecurityStamp = new Guid().ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = UserStatus.ACTIVE,
            };

            var result = await _userManager.CreateAsync(newUser, requestDto.Password);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
            }

            var roleResult = await _userManager.AddToRoleAsync(
                                newUser,
                                "User"
                            );

            return await GenerateJwtToken(newUser);
        }

        private async Task<LoginResponseDto> GenerateJwtToken(User user)
        {
            var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.UtcNow.AddMinutes(60 * 2), // 5 - 10mins
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                IsRevoked = false,
                UserId = user.Id,
                DateAdded = DateTime.UtcNow,
                DateExpire = DateTime.UtcNow.AddMonths(6),
                Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            var response = new LoginResponseDto()
            {
                AccessToken = jwtToken,
                RefreshToken = refreshToken.Token,
                ExpriresAt = token.ValidTo,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Avatar = user.Avatar,
                    Status = user.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = user.UpdatedAt,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                }
            };
            return response;
        }

    }
}
