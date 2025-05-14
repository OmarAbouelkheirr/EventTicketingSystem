namespace EventBookingSystem.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterDTO
    {
        public string FName { get; set; }
        public string LName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }  // "Organizer" or "Attendee"
        public string OrganizationName { get; set; }  // Only for Organizer
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}