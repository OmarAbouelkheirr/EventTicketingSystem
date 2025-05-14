using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MVC.DTOs;
using ZXing.Aztec.Internal;

namespace MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }



        public IActionResult Login()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]); // Same key used in token generation

                try
                {
                    var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                    var person = _context.Persons.FirstOrDefault(p => p.PersonID.ToString() == principal.FindFirstValue(ClaimTypes.NameIdentifier));
                   
                    if (person == null)
                    {
                        return RedirectToAction("Logout");
                    }

                    switch (principal.Claims.FirstOrDefault(c=>c.Type== ClaimTypes.Role).Value.ToLower())
                    {
                        case "admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "attendee":
                            return RedirectToAction("Dashboard", "Attendee");
                        case "organizer":
                            return RedirectToAction("Dashboard", "Organiser");
                    }
                }
                catch
                {
                    return RedirectToAction("Logout");
                }
            }
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }
        public async Task<IActionResult> Events()
        {
            var events =await _context.Events.Include(e=>e.Category).ToListAsync();

            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.PersonID == int.Parse(userIdClaim.Value));

                ViewBag.Person = person;
            }
            var categories = await _context.EventCategories.ToListAsync();

            ViewBag.Categories = categories;

            return View(events);
        }

        public async Task<IActionResult> HomePage()
        {
            var categories = await _context.EventCategories.ToListAsync();

            var events = await _context.Events
            .Include(e => e.Category)
            .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Events = events;


            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                var person = await _context.Persons.FirstOrDefaultAsync(p => p.PersonID == int.Parse(userIdClaim.Value));

                ViewBag.Person = person;
            }


            return View();
        }



        [Authorize]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("HomePage");
        }


        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO loginDto)
        {
            var person = await _context.Persons
                .FirstOrDefaultAsync(p => p.Email == loginDto.Email);

            if (person == null)
            {
                ViewBag.NoUser = "User doesn't exist";
                return View();
            }

            if (person.Password != loginDto.Password)
            {
                ViewBag.WrongPassword = "Wrong password";
                return View();
            }

            var token = GenerateJwtToken(person);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("jwt", token, cookieOptions);

            switch (person.Role.ToLower())
            {
                case "admin":
                    return RedirectToAction("Dashboard","Admin");
                case "attendee":
                    return RedirectToAction("Dashboard", "Attendee");
                case "organizer":
                    return RedirectToAction("Dashboard", "Organiser");
            }
            return Ok(new AuthResponseDTO
            {
                Role = person.Role,
                Message = "Login wans't successful"
            });
        }



        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDTO>> Register(RegisterDTO registerDto)
        {
            if (await _context.Persons.AnyAsync(p => p.Email == registerDto.Email))
            {
                ViewBag.Message = "Email already exists";
                return View();

            }

            if (registerDto.Role != "Organizer" && registerDto.Role != "Attendee")
            {

                ViewBag.Message = "Invalid Role";
                return View();
            }

            var person = new Person
            {
                FName = registerDto.FName,
                LName = registerDto.LName,
                Email = registerDto.Email,
                Password = registerDto.Password,
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


            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("jwt", token, cookieOptions);

            ViewBag.Success = "Account created successfully";
            return View();
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

            if (changePasswordDto.CurrentPassword != person.Password)
            {
                return BadRequest("Current password is incorrect");
            }

            person.Password = changePasswordDto.NewPassword;
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

    }
}
