namespace LeisureVolunteeringPlatform.DTOs
{
    public class RegisterForEventDTO
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public string EventDate { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public int Age { get; set; }
        public string? Comment { get; set; }
    }

}
