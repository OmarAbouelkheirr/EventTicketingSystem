using System.ComponentModel.DataAnnotations;

namespace EventBookingSystem.Models
{
    public class EventCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public string CategoryName { get; set; }

        public virtual ICollection<Event> Events { get; set; }
    }
}