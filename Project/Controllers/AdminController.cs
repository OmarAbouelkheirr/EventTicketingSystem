using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MVC.Controllers
{
    [Authorize(Roles ="Admin")]
    public class AdminController : Controller
    {

        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> DashBoard()
        {
            return View();
        }
        public async Task<IActionResult> EditEvent(int id)
        {
            ViewBag.EventId = id;

            var Event = await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventID == id);

            int maxAttendees = Event.MaxAttendees;
            int bookedTickets = await _context.Tickets
                .Where(t => t.EventID == id).CountAsync();

            if (Event == null)
                return RedirectToAction("DashBoard");

            return View(new EventUpdateDTO
            {
                EventName = Event.EventName,
                Description = Event.Description,
                EventDate = Event.EventDate,
                MaxAttendees = Event.MaxAttendees,
                Location = Event.Location,
                Image = Event.Image,
            });

        }

        public async Task<IActionResult> ViewCategory(int id)
        {
            var category = await _context.EventCategories.FirstOrDefaultAsync(x=>x.CategoryID==id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        public async Task<IActionResult> ViewEvent(int id)
        {
            var Event = await _context.Events
                .Include(e=>e.Category)
                .Include(e=>e.Organizer)
                .FirstOrDefaultAsync(e=>e.EventID==id);

            int maxAttendees = Event.MaxAttendees;
            int bookedTickets = await _context.Tickets
                .Where(t => t.EventID == id).CountAsync();

            if(Event==null)
                return RedirectToAction("DashBoard");

            return View(new EventResponseDTO
            {
                EventID = Event.EventID,
                EventName=Event.EventName,
                Description=Event.Description,
                EventDate=Event.EventDate,
                AssignedDate=Event.AssignedDate,
                MaxAttendees=Event.MaxAttendees,
                Location=Event.Location,
                Image=Event.Image,
                CategoryName=Event.Category.CategoryName,
                OrganizerName=Event.Organizer.OrganizationName,
                AvailableTickets= maxAttendees - bookedTickets
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin(string name, string email, string password)
        {
            if (await _context.Persons.AnyAsync(p => p.Email == email))
                return BadRequest("Email already exists");
            var person = new EventBookingSystem.Models.Person
            {
                FName = name,
                LName = name,
                Email = email,
                Password = password,
                Role = "Admin"
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            _context.Admins.Add(new EventBookingSystem.Models.Admin { PersonID = person.PersonID });
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> AddUser(string name, string email, string password)
        {
            if (await _context.Persons.AnyAsync(p => p.Email == email))
                return BadRequest("Email already exists");
            var person = new EventBookingSystem.Models.Person
            {
                FName = name,
                LName = name,
                Email = email,
                Password = password,
                Role = "Attendee"
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            _context.Attendees.Add(new EventBookingSystem.Models.Attendee { PersonID = person.PersonID });
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> AddOrganizer(string name, string email, string password)
        {
            if (await _context.Persons.AnyAsync(p => p.Email == email))
                return BadRequest("Email already exists");
            var person = new EventBookingSystem.Models.Person
            {
                FName = name,
                LName = name,
                Email = email,
                Password = password,
                Role = "Organizer"
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();
            _context.Organizers.Add(new EventBookingSystem.Models.Organizer { PersonID = person.PersonID, OrganizationName = name });
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
