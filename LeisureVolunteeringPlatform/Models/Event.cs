using System.ComponentModel.DataAnnotations;

namespace LeisureVolunteeringPlatform.Models
{
    public class Event
    {
        public int Id {  get; set; }

        public int OrganizerId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int VolunteersCount { get; set; }

        public string Address {  get; set; }

        public decimal Latitude { get; set; }
        
        public decimal Longitude { get; set; }

        public DateTime StartDate {get; set; }

        public DateTime EndDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
