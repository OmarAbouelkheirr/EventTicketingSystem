using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("upload-profile")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Validate type (e.g., only images)
            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG and PNG files are allowed.");

            // Generate hashed filename
            var hashedFileName = GenerateHashedFileName(file.FileName);
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/profile-images", hashedFileName);

            // Save file
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Store hashed file name or URL in DB (example: just return it)
            var publicUrl = $"/profile-images/{hashedFileName}";

            return Ok(new { imageUrl = publicUrl });
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


        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            var totalUsers = await _context.Persons.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalTickets = await _context.Tickets.CountAsync();
            var totalOrganisers = await _context.Organizers.CountAsync();

            return Ok(new
            {
                TotalOrganisers = totalOrganisers,
                TotalUsers = totalUsers,
                TotalEvents = totalEvents,
                TotalTickets = totalTickets
            });
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            var categories = await _context.EventCategories
                .Select(c => new
                {
                    c.CategoryID,
                     c.CategoryName,
                    c.Events.Count
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<ActionResult<EventCategoryDTO>> CreateCategory([FromBody] EventCategoryDTO categoryDto)
        {
            var category = new EventCategory
            {
                CategoryName = categoryDto.CategoryName
            };

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();

            categoryDto.CategoryID = category.CategoryID;
            return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryID }, categoryDto);
        }

        [HttpPut("categories/{id}")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] EventCategoryDTO categoryDto)
        {
            var category = await _context.EventCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.CategoryName = categoryDto.CategoryName;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("categories/{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.EventCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers([FromQuery]int page, [FromQuery]int pageSize)
        {

            var query = _context.Persons.AsQueryable();

            var skipCount = (page - 1) * pageSize;

            var users = await query.Skip(skipCount).Take(pageSize).Select(p => new
            {
                p.PersonID,
                p.FName,
                p.LName,
                p.Email,
                p.Role
            }).ToListAsync();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var person = await _context.Persons.FindAsync(id);
            if (person == null)
                return NotFound();

            _context.Persons.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("events")]
    //    [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<EventResponseDTO>>> GetEvents([FromQuery] string? name = null)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Select(e => new
                {
                    e.EventID,
                    e.EventName,
                    e.Organizer.OrganizationName,
                    e.Category.CategoryName,
                    e.EventDate,
                   
                })
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(e => e.EventName.Contains(name));
            }


            var res = await query.ToListAsync();
            return Ok(res);
        }

        [HttpPut("events/{id}")]
        public async Task<ActionResult> UpdateEvent(int id, [FromBody] EventUpdateDTO eventDto)
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
                return NotFound();

            eventEntity.EventName = eventDto.EventName;
            eventEntity.Description = eventDto.Description;
            eventEntity.EventDate = eventDto.EventDate;
            //eventEntity.MaxAttendees = eventDto.MaxAttendees;
            eventEntity.Location = eventDto.Location;
            //eventEntity.Image = eventDto.Image;
            //eventEntity.CategoryID = eventDto.CategoryID;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("events/{id}")]
        public async Task<ActionResult> DeleteEvent(int id)
        {
            var  eventEntity = await _context.Events.FirstOrDefaultAsync(x=>x.EventID==id);
            if (eventEntity == null)
                return NotFound();

            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("tickets")]
        public async Task<ActionResult<IEnumerable<object>>> GetTickets()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.Attendee)
                .ThenInclude(a => a.Person)
                .Select(t => new
                {
                    TicketID = t.TicketID,
                    EventName = t.Event.EventName,
                    EventDate = t.Event.EventDate,
                    Location = t.Event.Location,
                    BookingDate = t.BookingDate,
                    TicketStatus = (t.Event.EventDate < DateTime.Now) ? "Expired" : t.TicketStatus,
                    TicketCode = t.TicketCode,
                    AttendeeName = $"{t.Attendee.Person.FName} {t.Attendee.Person.LName}",
                    AttendeeEmail = t.Attendee.Person.Email
                })
                .ToListAsync();

            return Ok(tickets);
        }
        [HttpDelete("tickets/{id}")]
        public async Task<ActionResult<IEnumerable<object>>> DeleteTickets(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(x => x.TicketID == id);

                if (ticket is not null)
            {
                _context.Tickets.Remove(ticket);
              await  _context.SaveChangesAsync();
                return NoContent();
            }


            return NotFound();
        }

        [HttpGet("categories/{id}")]
        public async Task<ActionResult<EventCategory>> GetCategory(int id)
        {
            var category = await _context.EventCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<Person>> GetUser(int id)
        {
            var user = await _context.Persons.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
    }
}