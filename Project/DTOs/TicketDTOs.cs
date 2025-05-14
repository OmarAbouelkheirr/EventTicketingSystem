namespace EventBookingSystem.DTOs
{
    public class TicketBookingDTO
    {
        public int EventID { get; set; }
    }

    public class TicketResponseDTO
    {
        public int TicketID { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public DateTime BookingDate { get; set; }
        public string TicketStatus { get; set; }
        public string TicketCode { get; set; }
    }

    public class TicketStatisticsDTO
    {
        public int TotalTickets { get; set; }
        public int BookedTickets { get; set; }
        public int CancelledTickets { get; set; }
        public Dictionary<string, int> TicketsByEvent { get; set; }
    }
}