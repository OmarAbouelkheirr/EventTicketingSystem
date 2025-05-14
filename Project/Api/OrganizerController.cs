using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventBookingSystem.Controllers;
using System.Security.Claims;
using EventBookingSystem.Models;
namespace EventBookingSystem.Controllers
{
    [Route("api/organizer")]
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public OrganizerController(ApplicationDbContext context, IWebHostEnvironment env)
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
                var oldImagePath = Path.Combine(_env.WebRootPath,"profile-images", person.Image);

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

                person.Image= safeFileName;
                await _context.SaveChangesAsync(); 
                return Ok(new { safeFileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error saving file: " + ex.Message);
            }


        }


        [HttpGet("statistics")]
        public async Task<ActionResult<EventStatisticsDTO>> GetStatistics()
        {
            var organizerId =  await GetOrganizerId();
            var events = await _context.Events
            .Where(e => e.OrganizerID == organizerId)
            .Include(e => e.Category)
            .ToListAsync();

            var eventIds = events.Select(e => e.EventID).ToList();
            var tickets = await _context.Tickets
                .Where(t => eventIds.Contains(t.EventID))
                .ToListAsync();

            var statistics = new EventStatisticsDTO
            {
                TotalEvents = events.Count,
                TotalTickets = tickets.Count,
                TotalAttendees = tickets.Select(t => t.AttendeeID).Distinct().Count(),
                EventsByCategory = events
                    .GroupBy(e => e.Category.CategoryName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(statistics);
        }

        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<EventResponseDTO>>> GetMyEvents()
        {
            var organizerId = await GetOrganizerId();

            var events = await _context.Events
                .Where(e => e.OrganizerID == organizerId)
                .Include(e => e.Category)
                .Select(e => new EventResponseDTO
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    AssignedDate = e.AssignedDate,
                    MaxAttendees = e.MaxAttendees,
                    Location = e.Location,
                    Image = e.Image,
                    CategoryName = e.Category.CategoryName,
                    AvailableTickets = e.MaxAttendees - _context.Tickets.Count(t => t.EventID == e.EventID)
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpPut("events/{id}")]
        public async Task<ActionResult> UpdateEvent(int id, [FromBody] EventUpdateDTO eventDto)
        {
            var organizerId = await GetOrganizerId();

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizerId);

            if (eventEntity == null)
                return NotFound();

            eventEntity.EventName = eventDto.EventName;
            eventEntity.Description = eventDto.Description;
            eventEntity.EventDate = eventDto.EventDate;
            eventEntity.MaxAttendees = eventDto.MaxAttendees;
            eventEntity.Location = eventDto.Location;
            eventEntity.Image = eventDto.Image;
            eventEntity.CategoryID = eventDto.CategoryID;

            await _context.SaveChangesAsync();


            return RedirectToAction("EditEvent", "organiser", new { id = id });
        }

        [HttpDelete("events/{id}")]
        public async Task<ActionResult> DeleteEvent(int id)
        {
            var organizerId = await GetOrganizerId();

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizerId);

            if (eventEntity == null)
                return NotFound();

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("tickets")]
        public async Task<ActionResult<IEnumerable<TicketResponseDTO>>> GetEventTickets()
        {
            var organizerId = await GetOrganizerId();

            var tickets = await _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.Attendee)
                .ThenInclude(a => a.Person)
                .Where(t => t.Event.OrganizerID == organizerId)
                .Select(t => new TicketResponseDTO
                {
                    TicketID = t.TicketID,
                    EventName = t.Event.EventName,
                    EventDate = t.Event.EventDate,
                    Location = t.Event.Location,
                    BookingDate = t.BookingDate,
                    TicketStatus = (t.Event.EventDate < DateTime.Now) ? "Expired" : t.TicketStatus,
                    TicketCode = t.TicketCode
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpDelete("tickets/{id}")]
        public async Task<ActionResult<IEnumerable<TicketResponseDTO>>> GetEventTickets(int id)
        {
            var organizerId = await GetOrganizerId();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketID == id);
            if (ticket != null)
                _context.Tickets.Remove(ticket);
            else
                return BadRequest("Ticket doesnt exist");
            _context.SaveChanges();
            return Ok();
        }

        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] OrganizerProfileDTO profile)
        {
            var organizerId = await GetOrganizerId();

            var organizer = await _context.Organizers.Include(o => o.Person).FirstOrDefaultAsync(o => o.OrganizerID == organizerId);
            if (organizer == null)
                return NotFound();

            organizer.OrganizationName = profile.OrganizationName;
            organizer.Person.FName = profile.FName;
            organizer.Person.LName = profile.LName;
            organizer.Person.Email = profile.Email;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPut("password")]
        public async Task<ActionResult> UpdatePassword([FromBody]string password)
        {
            var organizerId = await GetOrganizerId();

            var organizer = await _context.Organizers.Include(o => o.Person).FirstOrDefaultAsync(o => o.OrganizerID == organizerId);
            
            if (organizer == null)
                return NotFound();

            organizer.Person.Password = password;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<int> GetOrganizerId()
        {
            var personId = GetCurrentUserId();
            var organizer = await _context.Organizers
                .FirstOrDefaultAsync(o => o.PersonID == personId);

            if (organizer == null)
                throw new UnauthorizedAccessException("User is not an organizer");

            return organizer.OrganizerID;
        }
    }
}