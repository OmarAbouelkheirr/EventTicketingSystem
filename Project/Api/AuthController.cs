using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventBookingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO loginDto)
        {
            Console.WriteLine("=== Login Attempt ===");
            Console.WriteLine($"Attempting login for email: {loginDto.Email}");

            var allPersons = await _context.Persons.ToListAsync();
            Console.WriteLine($"Total persons in database: {allPersons.Count}");
            foreach (var p in allPersons)
            {
                Console.WriteLine($"Found person: ID={p.PersonID}, Email={p.Email}, Role={p.Role}");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Email == loginDto.Email);

            if (person == null)
            {
                Console.WriteLine($"Person not found for email: {loginDto.Email}");
                return Unauthorized(new AuthResponseDTO
                {
                    Message = "Invalid email or password"
                });
            }

            Console.WriteLine($"Found person: ID={person.PersonID}, Role={person.Role}");
            var inputHash = HashPassword(loginDto.Password);
            Console.WriteLine($"Input password: {loginDto.Password}");
            Console.WriteLine($"Input password hash: {inputHash}");
            Console.WriteLine($"Stored password hash: {person.Password}");

            if (!VerifyPassword(loginDto.Password, person.Password))
            {
                Console.WriteLine("Password verification failed");
                return Unauthorized(new AuthResponseDTO
                {
                    Message = "Invalid email or password"
                });
            }

            Console.WriteLine("Password verified successfully");
            var token = GenerateJwtToken(person);
            Console.WriteLine("Token generated successfully");

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Role = person.Role,
                Message = "Login successful"
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDTO>> Register(RegisterDTO registerDto)
        {
            if (await _context.Persons.AnyAsync(p => p.Email == registerDto.Email))
            {
                return BadRequest(new AuthResponseDTO
                {
                    Message = "Email already exists"
                });
            }

            if (registerDto.Role != "Organizer" && registerDto.Role != "Attendee")
            {
                return BadRequest(new AuthResponseDTO
                {
                    Message = "Invalid role"
                });
            }

            var person = new Person
            {
                FName = registerDto.FName,
                LName = registerDto.LName,
                Email = registerDto.Email,
                Password = HashPassword(registerDto.Password),
                Role = registerDto.Role
            };

            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            if (registerDto.Role == "Organizer")
            {
                var organizer = new Organizer
                {
                    PersonID = person.PersonID,
                    OrganizationName = registerDto.OrganizationName
                };
                _context.Organizers.Add(organizer);
            }
            else if (registerDto.Role == "Attendee")
            {
                var attendee = new Attendee
                {
                    PersonID = person.PersonID
                };
                _context.Attendees.Add(attendee);
            }

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(person);

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Role = person.Role,
                Message = "Registration successful"
            });
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordDTO changePasswordDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var person = await _context.Persons.FindAsync(userId);

            if (person == null)
            {
                return Unauthorized();
            }

            if (!VerifyPassword(changePasswordDto.CurrentPassword, person.Password))
            {
                return BadRequest("Current password is incorrect");
            }

            person.Password = HashPassword(changePasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        private string GenerateJwtToken(Person person)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, person.PersonID.ToString()),
                new Claim(ClaimTypes.Email, person.Email),
                new Claim(ClaimTypes.Role, person.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}