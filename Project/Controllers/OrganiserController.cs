using System.Diagnostics;
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
using MVC.Models;

namespace MVC.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class OrganiserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public OrganiserController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            this._env = env;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }


        //gets user profile and categories before loading page
        public async Task<IActionResult> DashBoard()
        {
            var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var organizer = await _context.Organizers
                .Include(o => o.Person)
                .FirstOrDefaultAsync(o => o.PersonID == userid);


            Person person = organizer.Person;

            if (organizer == null)
            {
                return null;
            }

            var profile = new OrganizerProfileDTO
            {
                OrganizationName = organizer.OrganizationName,
                Email = person.Email,
                FName = person.FName,
                LName = person.LName,
                Image=person.Image
            };

            var categories = _context.EventCategories
            .Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName
            })
            .ToList();

            ViewBag.Profile = profile;
            ViewBag.Categories = categories;
            return View();
        }

        public async Task<IActionResult> ViewEvent(int id)
        {
            Event Event = await GetEvent(id);

            var EventModel = new EventResponseDTO()
            {
                EventName = Event.EventName,
                EventDate = Event.EventDate,
                Description = Event.Description,
                MaxAttendees = Event.MaxAttendees,
                Location = Event.Location,
                Image = Event.Image,
                CategoryName = Event.Category.CategoryName,
                OrganizerName = Event.Organizer.OrganizationName,
                AvailableTickets = -1
            };
            return View(EventModel);
        }
        public async Task<IActionResult> EditEvent(int id)
        {
            ViewBag.EventId = id;
            Event Event = await GetEvent(id);
            var EventModel = new EventUpdateDTO()
            {
                EventName = Event.EventName,
                EventDate = Event.EventDate,
                Description = Event.Description,
                MaxAttendees = Event.MaxAttendees,
                Location = Event.Location,
                Image = Event.Image,
                CategoryID = Event.CategoryID,
            };
            return View(EventModel);
        }
        public async Task<Event> GetEvent(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var organizer = await _context.Organizers.FirstOrDefaultAsync(o => o.PersonID == userId);

            if (organizer == null)
            {
                return null;
            }

            var eventItem = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizer.OrganizerID);

            if (eventItem == null)
            {
                return null;
            }

            return eventItem;
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

        public IActionResult Scan()
        {
            return View();
        }

        public async Task<ActionResult<object>> ScanTicket(string data)
        {
            try
            {
                var dict = data
                    .Split(';')
                    .Select(part => part.Split(':'))
                    .ToDictionary(split => split[0], split => split[1]);

                string ticketId = dict["TicketId"];
                string eventName = dict["Event"];
                string attendeeId = dict["AttendeeId"];

                var res = await _context.Tickets
                    .Include(t => t.Event)
                    .Include(t => t.Attendee)
                        .ThenInclude(a => a.Person)
                    .FirstOrDefaultAsync(t => t.TicketID.ToString() == ticketId && t.AttendeeID.ToString() == attendeeId);

                if (res == null || res.Event.EventName != eventName)
                {
                    return BadRequest("Wrong Ticket info");
                }
                else if (res.TicketStatus == "Scanned")
                {
                    return BadRequest(new
                    {
                        valid = false,
                        message = "This ticket is Scanned Before."
                    });
                }
                else if (res.Event.EventDate < DateTime.Now)
                {
                    return BadRequest(new
                    {
                        valid = false,
                        message = "This ticket is Expired."
                    });
                }
                else
                {
                    res.TicketStatus = "Scanned";
                    await _context.SaveChangesAsync();
                    return Ok(new
                    {
                        valid = true,
                        ticketId = ticketId,
                        eventName = eventName,
                        attendeeName = res.Attendee.Person.FName + " " + res.Attendee.Person.LName
                    });
                }
            }
            catch
            {
                return BadRequest(new
                {
                    valid = false,
                    message = "Not an event QR code."
                });
            }
        }



        public async Task<IActionResult> CreateEvent(EventCreateDTO newEvent)
        {

            var organizerId = await GetOrganizerId();

            var eventEntity = new Event
            {
                EventName = newEvent.EventName,
                Description = newEvent.Description,
                EventDate = newEvent.EventDate,
                AssignedDate = DateTime.UtcNow,
                MaxAttendees = newEvent.MaxAttendees,
                Location = newEvent.Location,
                CategoryID = newEvent.CategoryID,
                OrganizerID = organizerId,
            };



            var file = newEvent.FormFile;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");


            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Only JPEG and PNG files are allowed.");


            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var safeFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

            // Final save path in wwwroot (not in a subfolder)
            var savePath = Path.Combine(_env.WebRootPath, "event-images", safeFileName);

            try
            {
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                eventEntity.Image = safeFileName;
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error saving file: " + ex.Message);
            }


            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();

            return RedirectToAction("DashBoard", "Organiser");
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
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User is not authenticated");

            return int.Parse(userIdClaim.Value);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
