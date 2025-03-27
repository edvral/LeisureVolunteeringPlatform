namespace LeisureVolunteeringPlatform.Models
{
    public class LevelThreshold
    {
        public int Id { get; set; }
        public string LevelName { get; set; }
        public int MinPoints { get; set; }
        public int? MaxPoints { get; set; }
        public string Icon { get; set; }
    }
}
