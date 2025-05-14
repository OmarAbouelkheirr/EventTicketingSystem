using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [ForeignKey("Event")]
        public int EventID { get; set; }
        public virtual Event Event { get; set; }

        [ForeignKey("Attendee")]
        public int AttendeeID { get; set; }
        public virtual Attendee Attendee { get; set; }

        [Required]
        public string TicketStatus { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public string TicketCode { get; set; }
    }
}