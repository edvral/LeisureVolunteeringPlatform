namespace LeisureVolunteeringPlatform.DTOs
{
    public class EventDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int VolunteersCount { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string StartDate { get; set; } 
        public string EndDate { get; set; }
        public string StartTime { get; set; } 
        public string EndTime { get; set; } 
    }
}
