using System.ComponentModel.DataAnnotations;

namespace EventBookingSystem.Models
{
    public class Person
    {
        [Key]
        public int PersonID { get; set; }

        [Required]
        public string FName { get; set; }

        [Required]
        public string LName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } // "Admin", "Organizer", "Attendee"

        public string? Image { get; set; } = String.Empty;
    }
}