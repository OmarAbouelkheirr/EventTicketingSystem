using System.Drawing.Imaging;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Data;
using EventBookingSystem.DTOs;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC.DTOs;
using QRCoder;
using ZXing;
using ZXing.QrCode.Internal;

namespace MVC.Controllers
{
    [Authorize(Roles = "Attendee")]
    public class AttendeeController : Controller
    {

        private readonly ApplicationDbContext _context;
        public AttendeeController(ApplicationDbContext context)
        {
            _context = context;

        }
        public async Task<IActionResult> Dashboard()
        {
            return RedirectToAction("HomePage", "Account");
        }
        public async Task<IActionResult> ViewEvent(int id)
        {
            var resEvent = _context.Events.FirstOrDefault(e => e.EventID == id);
            return View(resEvent);
        }

        public async Task<IActionResult> BuyTicket(int id)
        {
            int attendeeId = await GetAttendeeId();
            var attendee = _context.Attendees.FirstOrDefault(a => a.PersonID == attendeeId);
            var targetEvent = _context.Events.FirstOrDefault(e => e.EventID == id);
            
            if (_context.Tickets.Any(t => t.EventID == id && t.AttendeeID == attendeeId))
            {
                ViewBag.Error = "You already bought this ticket already";
            }
            else
            {
                _context.Tickets.Add(new Ticket()
                {
                    EventID = id,
                    AttendeeID = attendeeId,
                    TicketStatus = "Active",
                    BookingDate = DateTime.Now,
                    TicketCode= $"TK-${new Random().Next(1,1000)}"
                });
                ViewBag.Message = "Purchase done successfully";
            }


                return RedirectToAction("ViewEvent",new { id =id});
        }

        public async Task<IActionResult> ViewTicket(int id)
        {
            int attendeeId = await GetAttendeeId();
            var attendee = _context.Attendees.FirstOrDefault(a => a.PersonID == attendeeId);
            var targetEvent = _context.Events.FirstOrDefault(e => e.EventID == id);

            if (_context.Tickets.Any(t => t.EventID == id && t.AttendeeID == attendeeId))
            {
                ViewBag.Message = "Purchase done successfully";
            }
            var ticket = _context.Tickets.Include(t=>t.Attendee).ThenInclude(a => a.Person).Include(t=>t.Event).FirstOrDefault(x=>x.TicketID==id);
            return View(ticket);
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

        [HttpGet("QRCode/{id}")]
        public async Task<IActionResult> QrCode(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.Attendee)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Create QR Code data to encode (ticket info)  
            var qrData = $"TicketId:{ticket.TicketID};Event:{ticket.Event.EventName};AttendeeId:{ticket.Attendee.AttendeeID}";
            return GenerateQrCode(qrData);

        }

        public IActionResult GenerateQrCode(string qrData)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData); // <-- Use this for PNG output
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            return File(qrCodeImage, "image/png");
        }

        public async Task<IActionResult> MyTickets()
        {
            var tickets = await GetTickets();
            ViewBag.Tickets =  tickets.Select(e => new TicketResponseDTO()
            {
                TicketID = e.TicketID,
                EventName = e.Event.EventName,
                EventDate = e.Event.EventDate,
                Location = e.Event.Location,
                BookingDate = e.BookingDate,
                TicketStatus = e.TicketStatus,
                TicketCode = e.TicketCode,
            }).ToList<TicketResponseDTO>();
            return View();
        }

        public ActionResult MyAccount()
        {
            var userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var person = _context.Persons.FirstOrDefault(x=>x.PersonID==userId);


            return View(person);


        }

        public async Task<IActionResult> ChangeProfileInfo(Person newPerson)
        {
            var userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var person = _context.Persons.FirstOrDefault(x => x.PersonID == userId);
            person.FName = newPerson.FName;
            person.LName = newPerson.LName;
            person.Email = newPerson.Email;
            await _context.SaveChangesAsync();

            return View("MyAccount",person);
        }

        public async Task<ActionResult> ChangePassword(ChangePasswordDTO password)
        {
            var userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var person = _context.Persons.FirstOrDefault(x => x.PersonID == userId);
            if (password.CurrentPassword != person.Password)
            {
                ViewBag.PasswordError = "Wrong current password";
            }
            else
            {
                person.Password = password.NewPassword;
                await _context.SaveChangesAsync();
                ViewBag.Success = "Password updated successfully";
            }

                return View("MyAccount",person);

        }

        public async Task<IEnumerable<Ticket>> GetTickets()
        {
            var userId =  int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var attendee = await _context.Attendees.FirstOrDefaultAsync(a => a.PersonID == userId);

            if (attendee == null)
            {
                return null;//Unauthorized();
            }

            var tickets = await _context.Tickets
                .Include(t => t.Event)
                .ThenInclude(e => e.Category)
                .Where(t => t.AttendeeID == attendee.AttendeeID)
                .ToListAsync();

            return tickets;
        }

        private async Task<int> GetAttendeeId()
        {
            var personId = GetCurrentUserId();

            var attendee =  await _context.Attendees
                .FirstOrDefaultAsync(a => a.PersonID == personId);

            if (attendee == null)
                throw new UnauthorizedAccessException("User is not an attendee");

            return attendee.AttendeeID;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User is not authenticated");

            return int.Parse(userIdClaim.Value);
        }
    }
}
