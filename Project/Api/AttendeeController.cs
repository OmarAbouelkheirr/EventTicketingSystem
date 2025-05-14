using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSystem.Controllers
{
    [Authorize(Roles = "Attendee")]
    [Route("api/attendee")]
    public class AttendeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public AttendeeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public static string GenerateHashedFileName(string originalFileName)
        {

            var extension = Path.GetExtension(originalFileName);
            using var sha256 = SHA256.Create();

            var raw = $"{originalFileName}_{DateTime.UtcNow.Ticks}";
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return $"{hash}{extension}";
        }

        public int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User is not authenticated");

            return int.Parse(userIdClaim.Value);
        }


        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");


            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG and PNG files are allowed.");

            var PersonId = GetCurrentUserId();
            var person = _context.Persons.FirstOrDefault(p => p.PersonID == PersonId);

            if (!string.IsNullOrEmpty(person.Image))
            {
                var oldImagePath = Path.Combine(_env.WebRootPath, "profile-images", person.Image);

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var safeFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

            // Final save path in wwwroot (not in a subfolder)
            var savePath = Path.Combine(_env.WebRootPath, "profile-images", safeFileName);

            try
            {
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                person.Image = safeFileName;
                await _context.SaveChangesAsync();
                return Ok(new { safeFileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error saving file: " + ex.Message);
            }


        }

        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents(
            [FromQuery] int? categoryId = null,
            [FromQuery] DateTime? date = null)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryID == categoryId);
            }

            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate.Date == date.Value.Date);
            }

            var events = await query.ToListAsync();
            return Ok(events);
        }

        [HttpPost("tickets")]
        public async Task<ActionResult<TicketResponseDTO>> BookTicket([FromBody] TicketBookingDTO bookingDto)
        {
            var attendeeId = await GetAttendeeId();

            // Check if event exists and has available tickets
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == bookingDto.EventID);

            if (eventEntity == null)
                return NotFound("Event not found");

            var ticketCount = await _context.Tickets
                .CountAsync(t => t.EventID == bookingDto.EventID);

            if (ticketCount >= eventEntity.MaxAttendees)
                return BadRequest("No tickets available");

            // Check if attendee already has a ticket for this event
            var existingTicket = await _context.Tickets
                .AnyAsync(t => t.EventID == bookingDto.EventID && t.AttendeeID == attendeeId);

            if (existingTicket)
                return BadRequest("You already have a ticket for this event");

            // Create ticket
            var ticket = new Ticket
            {
                EventID = bookingDto.EventID,
                AttendeeID = attendeeId,
                BookingDate = DateTime.UtcNow,
                TicketStatus = "Active",
                TicketCode = GenerateTicketCode()
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var response = new TicketResponseDTO
            {
                TicketID = ticket.TicketID,
                EventName = eventEntity.EventName,
                EventDate = eventEntity.EventDate,
                Location = eventEntity.Location,
                BookingDate = ticket.BookingDate,
                TicketStatus = ticket.TicketStatus,
                TicketCode = ticket.TicketCode
            };

            return CreatedAtAction(nameof(GetTickets), new { id = ticket.TicketID }, response);
        }

        [HttpGet("tickets")]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var attendee = await _context.Attendees.FirstOrDefaultAsync(a => a.PersonID == userId);

            if (attendee == null)
            {
                return Unauthorized();
            }

            var tickets = await _context.Tickets
                .Include(t => t.Event)
                .ThenInclude(e => e.Category)
                .Where(t => t.AttendeeID == attendee.AttendeeID)
                .ToListAsync();

            return Ok(tickets);
        }

        private async Task<int> GetAttendeeId()
        {
            var personId = GetCurrentUserId();
            var attendee = await _context.Attendees
                .FirstOrDefaultAsync(a => a.PersonID == personId);

            if (attendee == null)
                throw new UnauthorizedAccessException("User is not an attendee");

            return attendee.AttendeeID;
        }

        private string GenerateTicketCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}