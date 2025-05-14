using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using Microsoft.AspNetCore.Mvc;

namespace EventBookingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {


        private readonly ApplicationDbContext _context;
        public BaseApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User is not authenticated");

            return int.Parse(userIdClaim.Value);
        }


        protected string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedAccessException("User role not found");

            return roleClaim.Value;
        }

        protected string GetCurrentUserEmail()
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            if (emailClaim == null)
                throw new UnauthorizedAccessException("User email not found");

            return emailClaim.Value;
        }
    }
}