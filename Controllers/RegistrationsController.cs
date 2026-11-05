using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize]
public class RegistrationsController : Controller
{
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly RegistrationService _registrationService;
	private readonly TimeProvider _timeProvider;

	public RegistrationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
		RegistrationService registrationService, TimeProvider timeProvider)
	{
		_context = context;
		_userManager = userManager;
		_registrationService = registrationService;
		_timeProvider = timeProvider;
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(int id)
	{
		var currentUser = await _userManager.GetUserAsync(User);

		if (currentUser is null)
		{
			return Challenge();
		}

		var eventItem = await _context.Events.AsNoTracking().FirstOrDefaultAsync(eventItem => eventItem.Id == id);

		if (eventItem is null)
		{
			return NotFound();
		}

		var now = _timeProvider.GetLocalNow().DateTime;

		if (eventItem.Status != EventStatus.Published || eventItem.StartDateTime <= now)
		{
			TempData["ErrorMessage"] = "Registration is available only for a Published Event that has not started.";

			return RedirectToAction("Details", "Events", new { id });
		}

		var alreadyRegistered = await _context.Registrations.AsNoTracking().AnyAsync(registration =>
					registration.UserId == currentUser.Id && registration.EventId == id);

		if (alreadyRegistered)
		{
			TempData["ErrorMessage"] = "You are already registered for this Event.";

			return RedirectToAction("Details", "Events", new { id });
		}

		var currentRegistrationCount = await _context.Registrations.CountAsync(r => r.EventId == id);

		if (currentRegistrationCount >= eventItem.Capacity)
		{
			TempData["ErrorMessage"] = "No places are currently available.";

			return RedirectToAction("Details", "Events", new { id });
		}

		var result = await _registrationService.TryRegisterAsync(currentUser.Id, id);

		switch (result)
		{
			case RegistrationAttemptStatus.Success:
				TempData["StatusMessage"] = "Registration confirmed.";
				break;

			case RegistrationAttemptStatus.Duplicate:
				TempData["ErrorMessage"] = "You are already registered for this Event.";

				break;

			case RegistrationAttemptStatus.Full:
				TempData["ErrorMessage"] = "No places are currently available.";

				break;

			case RegistrationAttemptStatus.Unavailable:
				TempData["ErrorMessage"] = "Registration is no longer available for this Event.";

				break;

			case RegistrationAttemptStatus.EventNotFound:
				return NotFound();
		}

		return RedirectToAction("Details", "Events", new { id });
	}

	[HttpGet]
	public async Task<IActionResult> Mine()
	{
		var currentUser = await _userManager.GetUserAsync(User);

		if (currentUser is null)
		{
			return Challenge();
		}

		var registrations = await _context.Registrations.AsNoTracking().Include(registration => registration.Event)
					.ThenInclude(eventItem => eventItem.Category).Where(registration =>
					registration.UserId == currentUser.Id).OrderBy(registration => registration.Event.StartDateTime)
				.ToListAsync();

		return View(registrations);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Cancel(int id)
	{
		var currentUser = await _userManager.GetUserAsync(User);

		if (currentUser is null)
		{
			return Challenge();
		}

		var registration = await _context.Registrations.Include(registration => registration.Event)
				.FirstOrDefaultAsync(registration => registration.Id == id && registration.UserId == currentUser.Id);

		if (registration is null)
		{
			return NotFound();
		}

		if (registration.Event.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
		{
			TempData["ErrorMessage"] = "A Registration can be cancelled only before the Event begins.";

			return RedirectToAction(nameof(Mine));
		}

		_context.Registrations.Remove(registration);

		await _context.SaveChangesAsync();

		TempData["StatusMessage"] = "Your Registration was cancelled and the place is available again.";

		return RedirectToAction(nameof(Mine));
	}
}
