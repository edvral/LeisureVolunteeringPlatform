using LeisureVolunteeringPlatform.Data;
using LeisureVolunteeringPlatform.DTOs;
using LeisureVolunteeringPlatform.Models;
using LeisureVolunteeringPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LeisureVolunteeringPlatform.Controllers
{

    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model, [FromServices] EmailService emailService)
        {
            if (string.IsNullOrEmpty(model.Email) || !model.Email.Contains("@"))
            {
                return BadRequest(new { message = "Netinkamas el. pašto formatas! (Turi būti @ simbolis)" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return Conflict(new { message = "Toks El. paštas jau egzistuoja!" });

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return Conflict(new { message = "Toks vartotojo vardas jau egzistuoja!" });

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Slaptažodžiai nesutampa!" });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            string verificationToken = GenerateEmailVerificationToken();

            var user = new User
            {
                Email = model.Email,
                Username = model.Username,
                PasswordHash = passwordHash,
                Role = model.Role,
                EmailVerified = false,
                VerificationToken = verificationToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await emailService.SendVerificationEmail(user.Email, verificationToken);

            return Ok(new { message = "Registracija sėkminga. Patikrinkite savo el. paštą, kad patvirtintumėte paskyrą." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string token)
        {

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Netinkama ar jau panaudota patvirtinimo nuoroda.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null)
            {
                return BadRequest("Netinkama ar jau panaudota patvirtinimo nuoroda.");
            }

            user.EmailVerified = true;
            user.VerificationToken = null;
            await _context.SaveChangesAsync();

            return Redirect("http://localhost:8080/auth?verified=true");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized(new { message = "Neteisingi prisijungimo duomenys!" });

            if (!user.EmailVerified)
                return Unauthorized(new { message = "Prašome patvirtinti savo el. paštą!" });

            bool rememberMe = model.RememberMe;

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            SaveRefreshToken(user.Id, refreshToken);

            return Ok(new { token, refreshToken, rememberMe });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Neautorizuotas naudotojas." });
            }

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedToken == null || storedToken.Revoked)
            {
                return BadRequest(new { message = "Netinkamas atsinaujinimo žetonas." });
            }

            storedToken.Revoked = true;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Naudotojas atisijungė sėkmingai!" });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.Revoked);

            if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Netinkamas arba nebegaliojantis atsinaujinimo žetonas!." });
            }

            storedToken.Revoked = true;

            var user = await _context.Users.FindAsync(storedToken.UserId);
            if (user == null)
            {
                return Unauthorized(new { message = "Naudotojas nerastas." });
            }


            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            var newTokenEntry = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                Expires = DateTime.Now.AddDays(7)
            };

            _context.RefreshTokens.Add(newTokenEntry);
            await _context.SaveChangesAsync();

            return Ok(new { token = newAccessToken, refreshToken = newRefreshToken });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model, [FromServices] EmailService emailService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Vartotojas su tokiu el. paštu nerastas." });
            }

            string resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.ResetPasswordToken = resetToken;
            user.ResetTokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            string resetLink = $"http://localhost:8080/reset-password?token={WebUtility.UrlEncode(resetToken)}";
            await emailService.SendPasswordResetEmail(user.Email, resetLink);

            return Ok(new { message = "Slaptažodžio atkūrimo nuoroda išsiųsta!" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == model.Token && u.ResetTokenExpiry > DateTime.Now);
            if (user == null)
            {
                return BadRequest(new { message = "Netinkama arba pasibaigusi slaptažodžio atkūrimo nuoroda." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Slaptažodis sėkmingai pakeistas!" });
        }


        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim("userId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenExpiration = DateTime.Now.AddHours(1); 

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: tokenExpiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        private void SaveRefreshToken(int userId, string refreshToken)
        {
            var tokenEntry = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                Expires = DateTime.Now.AddDays(7)
            };

            _context.RefreshTokens.Add(tokenEntry);
            _context.SaveChanges();
        }

        private string GenerateEmailVerificationToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            string token = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            return token;
        }
    }
}
