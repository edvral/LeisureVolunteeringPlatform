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
using System.Globalization;

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
                EndTime = e.EndTime.ToString(@"hh\:mm"),
                e.OrganizerId
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
                e.OrganizerId
            })
            .FirstOrDefaultAsync();

        if (eventData == null) return NotFound("Veikla nerasta!");

        var volunteersPerDate = await _context.EventRegistrations
            .Where(er => er.EventId == id && er.IsApproved == true)
            .GroupBy(er => er.EventDate)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                ApprovedCount = g.Count(),
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
        var volunteerApprovalStatus = new Dictionary<string, string>();
        var volunteerFeedback = new Dictionary<string, string>();

        if (userId > 0)
        {
            var userRegistrations = await _context.EventRegistrations
                .Where(er => er.EventId == id && er.UserId == userId)
                .Select(er => new { er.EventDate, er.IsApproved, er.Feedback })
                .ToListAsync();

            volunteerApprovalStatus = userRegistrations
                .ToDictionary(r => r.EventDate.ToString("yyyy-MM-dd"), r => r.IsApproved == null ? "pending" : (r.IsApproved == true ? "approved" : "rejected"));

            volunteerFeedback = userRegistrations
                .ToDictionary(r => r.EventDate.ToString("yyyy-MM-dd"), r => r.Feedback ?? "");

            pendingRegistrations = userRegistrations
                .Where(r => r.IsApproved == null)
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
            eventData.OrganizerId,
            VolunteersCountPerDate = volunteersPerDate.ToDictionary(v => v.Date, v => Math.Max(0, eventData.VolunteersCount - v.ApprovedCount)),
            PendingRegistrations = pendingRegistrations,
            VolunteerApprovalStatus = volunteerApprovalStatus,
            VolunteerFeedback = volunteerFeedback
        };

        return Ok(response);
    }
  
    [Authorize(Policy = "OrganizerOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventDTO eventDto)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        if (userId == 0) return Unauthorized(new { message = "Neautorizuotas naudotojas." });

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
            EndTime = endTime,
            OrganizerId = userId
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, new
        {
            message = "Veikla pridėta sėkmingai!",
            eventId = newEvent.Id
        });
    }

    [Authorize(Policy = "OrganizerOnly")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDTO eventDto)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        if (userId == 0) return Unauthorized(new { message = "Neautorizuotas naudotojas." });

        var eventToUpdate = await _context.Events.FindAsync(id);
        if (eventToUpdate == null) return NotFound(new { message = "Veikla nerasta!" });

        if (eventToUpdate.OrganizerId != userId)
            return Forbid();

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

        if (!DateTime.TryParseExact(eventDto.StartDate, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal, out DateTime startDate))
            return UnprocessableEntity(new { message = "Netinkama pradžios data!" });

        if (!DateTime.TryParseExact(eventDto.EndDate, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal, out DateTime endDate))
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

        eventToUpdate.Name = eventDto.Name;
        eventToUpdate.Description = eventDto.Description;
        eventToUpdate.VolunteersCount = eventDto.VolunteersCount;
        eventToUpdate.Address = eventDto.Address;
        eventToUpdate.Latitude = eventDto.Latitude;
        eventToUpdate.Longitude = eventDto.Longitude;
        eventToUpdate.StartDate = startDate;
        eventToUpdate.EndDate = endDate;
        eventToUpdate.StartTime = startTime;
        eventToUpdate.EndTime = endTime;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Veikla atnaujinta sėkmingai!" });
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

        int approvedCount = await _context.EventRegistrations
        .Where(er => er.EventId == registrationDto.EventId && er.EventDate == parsedEventDate && er.IsApproved == true)
        .CountAsync();

        var newRegistration = new EventRegistration
        {
            UserId = userId,
            EventId = registrationDto.EventId,
            EventDate = parsedEventDate,
            Name = registrationDto.Name,
            Surname = registrationDto.Surname,
            Age = registrationDto.Age,
            Comment = registrationDto.Comment,
            IsApproved = null
        };

        _context.EventRegistrations.Add(newRegistration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registracija į savanorišką veiklą pateikta!", eventDate = registrationDto.EventDate, pendingApproval = true });
    }

    [Authorize(Policy = "OrganizerOnly")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        if (userId == 0) return Unauthorized(new { message = "Neautorizuotas naudotojas." });

        var eventToDelete = await _context.Events.FindAsync(id);
        if (eventToDelete == null) return NotFound(new { message = "Veikla nerasta!" });

        if (eventToDelete.OrganizerId != userId)
            return Forbid();

        var eventRegistrations = _context.EventRegistrations.Where(er => er.EventId == id);
        _context.EventRegistrations.RemoveRange(eventRegistrations);

        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Veikla sėkmingai ištrinta!" });
    }

    [Authorize(Policy = "OrganizerOnly")]
    [HttpGet("{id}/volunteers/{eventDate}")]
    public async Task<IActionResult> GetEventVolunteers(int id, string eventDate)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        var eventEntity = await _context.Events.FindAsync(id);

        if (eventEntity == null)
            return NotFound(new { message = "Veikla nerasta!" });

        if (eventEntity.OrganizerId != userId)
            return Forbid();

        if (!DateTime.TryParseExact(eventDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime parsedDate))
        {
            return BadRequest(new { message = "Netinkamas datos formatas! Turi būti YYYY-MM-DD." });
        }

        var volunteers = await _context.EventRegistrations
            .Where(er => er.EventId == id && er.EventDate == parsedDate)
            .Select(er => new
            {
                registrationId = er.Id,
                er.Name,
                er.Surname,
                er.Age,
                er.Comment,
                er.IsApproved,
                er.Feedback,
                er.EventDate
            })
            .ToListAsync();

        return Ok(volunteers);
    }

    [Authorize(Policy = "OrganizerOnly")]
    [HttpPost("{id}/volunteers/{registrationId}/approve")]
    public async Task<IActionResult> ApproveVolunteer(int id, int registrationId, [FromBody] VolunteerApprovalDTO approvalDto)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        var eventEntity = await _context.Events.FindAsync(id);

        if (eventEntity == null)
            return NotFound(new { message = "Veikla nerasta!" });

        if (eventEntity.OrganizerId != userId)
            return Forbid(); 

        var registration = await _context.EventRegistrations.FindAsync(registrationId);
        if (registration == null || registration.EventId != id)
            return NotFound(new { message = "Registracija nerasta!" });

        int approvedCount = await _context.EventRegistrations
        .Where(er => er.EventId == id && er.EventDate == registration.EventDate && er.IsApproved == true)
        .CountAsync();

        if (approvalDto.IsApproved == true && approvedCount >= eventEntity.VolunteersCount)
        {
            return BadRequest(new { message = "Nebėra laisvų vietų šiai datai!" });
        }

        registration.IsApproved = approvalDto.IsApproved;
        registration.Feedback = approvalDto.Feedback;

        await _context.SaveChangesAsync();

        return Ok(new { message = approvalDto.IsApproved ? "Savanoris patvirtintas!" : "Savanoris atmestas!" });
    }

    [Authorize(Policy = "VolunteerOnly")]
    [HttpPost("{eventId}/cancel-registration/{eventDate}")]
    public async Task<IActionResult> CancelRegistration(int eventId, string eventDate)
    {
        var userId = int.Parse(User.FindFirst("userId")?.Value);

        if (!DateTime.TryParseExact(eventDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
        {
            return BadRequest(new { message = "Netinkamas datos formatas!" });
        }

        var registration = await _context.EventRegistrations
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId && er.EventDate == parsedDate);

        if (registration == null)
        {
            return NotFound(new { message = "Registracija nerasta!" });
        }

        var eventDetails = await _context.Events.FindAsync(eventId);
        if (eventDetails == null)
        {
            return NotFound(new { message = "Veikla nerasta!" });
        }

        if (!TimeSpan.TryParse(eventDetails.StartTime.ToString(), out TimeSpan eventStartTime))
        {
            return BadRequest(new { message = "Netinkamas laiko formatas!" });
        }

        DateTime eventStartDateTime = parsedDate.Add(eventStartTime);

        if ((eventStartDateTime - DateTime.UtcNow).TotalHours <= 24)
        {
            return BadRequest(new { message = "Negalite atšaukti registracijos likus mažiau nei 24 valandoms iki renginio pradžios!" });
        }

        _context.EventRegistrations.Remove(registration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registracija sėkmingai atšaukta!" });
    }
}
