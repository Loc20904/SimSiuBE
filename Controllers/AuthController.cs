using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ViettalAPI.Data;
using ViettalAPI.DTOs;
using ViettalAPI.Models;

namespace ViettalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ViettalDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ViettalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email và mật khẩu không được để trống." });
            }

            var emailNormalized = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return BadRequest(new { message = "Email hoặc mật khẩu không đúng." });
            }

            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString().ToLower() // matches customer/admin in Flutter
                }
            };

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password) || 
                string.IsNullOrWhiteSpace(request.FullName) || 
                string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ các trường thông tin." });
            }

            var emailNormalized = request.Email.Trim().ToLower();
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (emailExists)
            {
                return BadRequest(new { message = "Email này đã được sử dụng." });
            }

            var user = new AppUser
            {
                Id = "user-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLower(),
                Phone = request.Phone.Trim(),
                Role = UserRole.Customer,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString().ToLower()
                }
            };

            return Ok(response);
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyStr = jwtSettings["Key"] ?? "SuperSecretKeyForViettalSimDepProject2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Admin or Customer
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "ViettalAPI",
                audience: jwtSettings["Audience"] ?? "ViettalClient",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
