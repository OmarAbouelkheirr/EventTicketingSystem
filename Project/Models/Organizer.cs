using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Organizer
    {
        [Key]
        public int OrganizerID { get; set; }

        [ForeignKey("Person")]
        public int PersonID { get; set; }
        public virtual Person Person { get; set; }

        public string OrganizationName { get; set; }

        public virtual ICollection<Event> Events { get; set; }
    }
}