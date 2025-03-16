using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LeisureVolunteeringPlatform.Data;
using LeisureVolunteeringPlatform.Models;
using LeisureVolunteeringPlatform.DTOs;
using System;
using Microsoft.AspNetCore.Authorization;

[Route("api/events")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EventController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .Select(e => new {
                e.Id,
                e.Name,
                e.Description,
                e.VolunteersCount,
                e.Address,
                e.Latitude,
                e.Longitude,
                StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                EndDate = e.EndDate.ToString("yyyy-MM-dd"),
                StartTime = e.StartTime.ToString(@"hh\:mm"),
                EndTime = e.EndTime.ToString(@"hh\:mm")
            }).ToListAsync();

        return Ok(events);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventData = await _context.Events
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Description,
                e.VolunteersCount,
                e.Address,
                e.Latitude,
                e.Longitude,
                StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                EndDate = e.EndDate.ToString("yyyy-MM-dd"),
                StartTime = e.StartTime.ToString(@"hh\:mm"),    
                EndTime = e.EndTime.ToString(@"hh\:mm"),
             })
            .FirstOrDefaultAsync();

        if (eventData == null) return NotFound("Veikla nerasta!");

        var volunteersPerDate = await _context.EventRegistrations
            .Where(er => er.EventId == id)
            .GroupBy(er => er.EventDate)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                RegisteredCount = g.Count(),
            })
            .ToListAsync();

        int userId = 0;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                userId = int.Parse(userIdClaim);
            }
        }


        var pendingRegistrations = new Dictionary<string, bool>();

        if (userId > 0)
        {
            var userRegistrations = await _context.EventRegistrations
                .Where(er => er.EventId == id && er.UserId == userId)
                .Select(er => new { er.EventDate, er.IsApproved })
                .ToListAsync();

            pendingRegistrations = userRegistrations
                .Where(r => !r.IsApproved)
                .ToDictionary(r => r.EventDate.ToString("yyyy-MM-dd"), r => true);
        }

        var response = new
        {
            eventData.Id,
            eventData.Name,
            eventData.Description,
            eventData.VolunteersCount,
            eventData.Address,
            eventData.Latitude,
            eventData.Longitude,
            eventData.StartDate,
            eventData.EndDate,
            eventData.StartTime,
            eventData.EndTime,
            VolunteersCountPerDate = volunteersPerDate.ToDictionary(v => v.Date, v => Math.Max(0, eventData.VolunteersCount - v.RegisteredCount)),
            PendingRegistrations = pendingRegistrations
        };

        return Ok(response);
    }
  
    [Authorize(Policy = "OrganizerOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventDTO eventDto)
    {
        if (eventDto == null)
            return BadRequest(new { message = "Netinkami veiklos duomenys!" });

        if (string.IsNullOrWhiteSpace(eventDto.Name) ||
            string.IsNullOrWhiteSpace(eventDto.Description) ||
            string.IsNullOrWhiteSpace(eventDto.Address) ||
            string.IsNullOrWhiteSpace(eventDto.StartDate) ||
            string.IsNullOrWhiteSpace(eventDto.EndDate) ||
            string.IsNullOrWhiteSpace(eventDto.StartTime) ||
            string.IsNullOrWhiteSpace(eventDto.EndTime) ||
            eventDto.Latitude == 0 ||
            eventDto.Longitude == 0 ||
            eventDto.VolunteersCount < 1)
        {
            return UnprocessableEntity(new { message = "Visi laukai yra privalomi, o savanorių skaičius turi būti bent 1!" });
        }

        if (!DateTime.TryParseExact(eventDto.StartDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime startDate))
            return UnprocessableEntity(new { message = "Netinkama pradžios data!" });

        if (!DateTime.TryParseExact(eventDto.EndDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime endDate))
            return UnprocessableEntity(new { message = "Netinkama pabaigos data!" });

        if (startDate.Date < DateTime.Today)
            return UnprocessableEntity(new { message = "Pradžios data negali būti praeityje!" });

        if (endDate < startDate)
            return UnprocessableEntity(new { message = "Pabaigos data negali būti ankstesnė nei pradžios data!" });

        if (!TimeSpan.TryParse(eventDto.StartTime, out TimeSpan startTime) || !TimeSpan.TryParse(eventDto.EndTime, out TimeSpan endTime))
            return UnprocessableEntity(new { message = "Netinkama laiko reikšmė!" });

        if (endTime <= startTime)
            return UnprocessableEntity(new { message = "Pabaigos laikas turi būti vėliau nei pradžios laikas!" });

        if (startDate == DateTime.Today && startTime < DateTime.Now.TimeOfDay)
            return UnprocessableEntity(new { message = "Jei veikla vyksta šiandien, pradžios laikas turi būti ateityje!" });

        var newEvent = new Event
        {
            Name = eventDto.Name,
            Description = eventDto.Description,
            VolunteersCount = eventDto.VolunteersCount,
            Address = eventDto.Address,
            Latitude = eventDto.Latitude,
            Longitude = eventDto.Longitude,
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startTime,
            EndTime = endTime
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, new
        {
            message = "Veikla pridėta sėkmingai!",
            eventId = newEvent.Id
        });
    }

    [Authorize(Policy = "VolunteerOnly")]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterForEvent([FromBody] RegisterForEventDTO registrationDto)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        if (userId == 0) return Unauthorized(new { message = "Neautorizuotas naudotojas." });

        if (!DateTime.TryParseExact(registrationDto.EventDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime parsedEventDate))
        {
            return UnprocessableEntity(new { message = "Netinkamas datos formatas! Turi būti YYYY-MM-DD." });
        }

        var existingRegistration = await _context.EventRegistrations
            .FirstOrDefaultAsync(er => er.UserId == userId && er.EventId == registrationDto.EventId && er.EventDate == parsedEventDate);

        if (existingRegistration != null)
            return Conflict(new { message = "Jūs jau užsiregistravote šiai datai!" });

        var eventEntity = await _context.Events.FindAsync(registrationDto.EventId);
        if (eventEntity == null)
            return NotFound(new { message = "Veikla nerasta!" });

        int registeredCount = await _context.EventRegistrations
            .CountAsync(er => er.EventId == registrationDto.EventId && er.EventDate == parsedEventDate);

        if (registeredCount >= eventEntity.VolunteersCount)
            return UnprocessableEntity(new { message = "Nebėra laisvų vietų šiai datai!" });

        var newRegistration = new EventRegistration
        {
            UserId = userId,
            EventId = registrationDto.EventId,
            EventDate = parsedEventDate,
            Name = registrationDto.Name,
            Surname = registrationDto.Surname,
            Age = registrationDto.Age,
            Comment = registrationDto.Comment,
            IsApproved = false 
        };

        _context.EventRegistrations.Add(newRegistration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registracija į savanorišką veiklą pateikta!", eventDate = registrationDto.EventDate });
    }
}
