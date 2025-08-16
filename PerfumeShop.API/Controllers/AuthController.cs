using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PerfumeShop.API.Models;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthController(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _unitOfWork.Users.FindAsync(u => u.Email == model.Email);
            if (userExists.Any())
                return StatusCode(StatusCodes.Status400BadRequest, new { Status = "Error", Message = "User already exists!" });

            // Hash the password
            string hashedPassword = HashPassword(model.Password);

            User user = new User()
            {
                Email = model.Email,
                Password = hashedPassword, // Store hashed password
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            Console.WriteLine($"Login attempt: {model.Email}");
            
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == model.Email);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                Console.WriteLine($"User not found: {model.Email}");
                return Unauthorized(new { Status = "Error", Message = "Invalid email or password!" });
            }

            Console.WriteLine($"User found: {user.Email}, IsAdmin: {user.IsAdmin}");
            
            // Verify the password
            bool passwordValid = VerifyPassword(model.Password, user.Password);
            Console.WriteLine($"Password valid: {passwordValid}");
            
            if (!passwordValid)
                return Unauthorized(new { Status = "Error", Message = "Invalid email or password!" });

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (user.IsAdmin)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else
            {
                authClaims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var token = GenerateToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                userId = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                isAdmin = user.IsAdmin
            });
        }

        private JwtSecurityToken GenerateToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hash = Convert.ToBase64String(hashedBytes);
                Console.WriteLine($"AuthController - Password: {password}, Hash: {hash}");
                // Der Hash für Admin123 muss O2Esdae1BIpDX7bsgeUv+S1teVqLWpwXBw9qY8l6U7I= sein
                return hash;
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var newHash = HashPassword(password);
            Console.WriteLine($"Input hash: {newHash}");
            Console.WriteLine($"Stored hash: {hashedPassword}");
            return newHash.Equals(hashedPassword);
        }
    }
} 