using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LeisureVolunteeringPlatform.Models
{
    public class EventRegistration
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int EventId { get; set; }

        public DateTime EventDate { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        public string Name { get; set; }

        public string Surname { get; set; }

        public int Age { get; set; }

        public string? Comment { get; set; }

        public bool? IsApproved { get; set; }
        public string? Feedback { get; set; }

        public User User { get; set; }

        public Event Event { get; set; }
    }
}
