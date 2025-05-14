namespace EventBookingSystem.DTOs
{
    public class EventCreateDTO
    {

        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
        public string Location { get; set; }
        public IFormFile FormFile { get; set; }
        public int CategoryID { get; set; }
        public int TicketPrice { get; set; }
    }

    public class EventUpdateDTO
    {
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public int MaxAttendees { get; set; }
        public string Location { get; set; }
        public string Image { get; set; }
        public int CategoryID { get; set; }
    }

    public class EventResponseDTO
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime AssignedDate { get; set; }
        public int MaxAttendees { get; set; }
        public string Location { get; set; }
        public string Image { get; set; }
        public string CategoryName { get; set; }
        public string OrganizerName { get; set; }
        public int AvailableTickets { get; set; }
    }

    public class EventCategoryDTO
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
    }

    public class EventStatisticsDTO
    {
        public int TotalEvents { get; set; }
        public int TotalTickets { get; set; }
        public int TotalAttendees { get; set; }
        public Dictionary<string, int> EventsByCategory { get; set; }
    }
}