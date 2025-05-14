using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        public string EventName { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public DateTime AssignedDate { get; set; }

        [Required]
        public int MaxAttendees { get; set; }

        [Required]
        public string Location { get; set; }

        public string Image { get; set; }


        [ForeignKey("Organizer")]
        public int OrganizerID { get; set; }
        public virtual Organizer Organizer { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }
        public virtual EventCategory Category { get; set; }
    }
}