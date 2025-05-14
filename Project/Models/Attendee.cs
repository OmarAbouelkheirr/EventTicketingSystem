using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Attendee
    {
        [Key]
        public int AttendeeID { get; set; }

        [ForeignKey("Person")]
        public int PersonID { get; set; }
        public virtual Person Person { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}