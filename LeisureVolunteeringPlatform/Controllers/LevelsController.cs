using LeisureVolunteeringPlatform.Data;
using Microsoft.AspNetCore.Mvc;
using LeisureVolunteeringPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LeisureVolunteeringPlatform.Controllers
{
    [Route("api/levels")]
    [ApiController]
    public class LevelsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LevelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLevels()
        {
            var levels = await _context.LevelThresholds
                .OrderBy(l => l.MinPoints)
                .Select(l => new
                {
                    l.Id,
                    LevelName = l.LevelName,
                    MinPoints = l.MinPoints,
                    MaxPoints = l.MaxPoints,
                    Icon = l.Icon
                })
                .ToListAsync();

            return Ok(levels);
        }
    }
}
