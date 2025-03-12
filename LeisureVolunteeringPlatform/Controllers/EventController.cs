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
                EndTime = e.EndTime.ToString(@"hh\:mm")      
            })
            .FirstOrDefaultAsync();

        if (eventData == null) return NotFound("Veikla nerasta!");

        return Ok(eventData);
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
}
