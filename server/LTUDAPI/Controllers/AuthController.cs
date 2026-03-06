using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LTUDAPI.Data;
using LTUDAPI.DTOs;

namespace LTUDAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context) { _context = context; }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Tìm user dựa trên username và password_hash (trong thực tế nên dùng BCrypt)
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == request.Username && a.PasswordHash == request.Password);

            if (account == null) return Unauthorized("Tài khoản hoặc mật khẩu sai.");

            return Ok(new { 
                IdAcc = account.IdAcc, 
                Username = account.Username,
                Role = account.IdRole 
            });
        }
    }

    public class LoginRequest {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}