using LeisureVolunteeringPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace LeisureVolunteeringPlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<LevelThreshold> LevelThresholds { get; set; }
    }
}
