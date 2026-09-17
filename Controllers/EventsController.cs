using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;

	public EventsController(
		ILogger<EventsController> logger,
		ApplicationDbContext context)
	{
		_logger = logger;
		_context = context;
	}

	public async Task<IActionResult> Index()
	{
		_logger.LogInformation("Loading Published upcoming CEMS events.");

		var now = DateTime.Now;

		var events = await _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem =>
				eventItem.Status == EventStatus.Published &&
				eventItem.StartDateTime > now)
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ToListAsync();

		return View(events);
	}

	public async Task<IActionResult> Details(int id)
	{
		var eventItem = await _context.Events
			.AsNoTracking()
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.SingleOrDefaultAsync(item =>
				item.Id == id &&
				item.Status == EventStatus.Published);

		if (eventItem is null)
		{
			Response.StatusCode = StatusCodes.Status404NotFound;
			return View("EventNotFound");
		}

		return View(eventItem);
	}
}
