using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using CampusEvents.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace CampusEvents.Controllers;

public class EventsController : Controller
{

private readonly TimeProvider _timeProvider;
	private const int DefaultPageSize = 10;
	private readonly ILogger<EventsController> _logger;
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private static readonly int[] AllowedPageSizes =
		{
		10,
		25,
		50
	};

	public EventsController(
		ILogger<EventsController> logger,
		TimeProvider timeProvider,
		ApplicationDbContext context,
		UserManager<ApplicationUser> userManager)
	{
		_timeProvider = timeProvider;
		_logger = logger;
		_context = context;
		_userManager = userManager;
	}

[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Draft)
	{
		TempData["ErrorMessage"] = "Only a Draft Event can be permanently deleted.";
		return RedirectToAction(nameof(Manage));
	}

	_context.Events.Remove(eventItem);
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was permanently deleted.";
	return RedirectToAction(nameof(Manage));
}

[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Delete(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events
		.AsNoTracking()
		.Include(item => item.Category)
		.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id && item.Status == EventStatus.Draft);

	if (eventItem is null) return EventNotFound();

	return View(eventItem);
}

[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Publish(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Draft)
	{
		TempData["ErrorMessage"] = "Only a Draft Event can be published.";
		return RedirectToAction(nameof(Manage));
	}

	var now = _timeProvider.GetLocalNow().DateTime;
	if (eventItem.StartDateTime <= now)
	{
		TempData["ErrorMessage"] = "A Draft Event can be published only before its start date and time.";
		return RedirectToAction(nameof(Manage));
	}

	eventItem.Status = EventStatus.Published;
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was published.";
	return RedirectToAction(nameof(Manage));
}
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Cancel(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status != EventStatus.Published)
	{
		TempData["ErrorMessage"] = "Only a Published Event can be cancelled.";
		return RedirectToAction(nameof(Manage));
	}

	var now = _timeProvider.GetLocalNow().DateTime;
	if (eventItem.StartDateTime <= now)
	{
		TempData["ErrorMessage"] = "A Published Event can be cancelled only before it starts.";
		return RedirectToAction(nameof(Manage));
	}

	eventItem.Status = EventStatus.Cancelled;
	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was cancelled.";
	return RedirectToAction(nameof(Manage));
}
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, EventInputModel model)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);
	if (eventItem is null) return EventNotFound();

	if (eventItem.Status == EventStatus.Cancelled)
	{
		TempData["ErrorMessage"] = "A Cancelled Event can no longer be edited.";
		return RedirectToAction(nameof(Manage));
	}

	if (eventItem.Status != EventStatus.Draft && eventItem.Status != EventStatus.Published) return EventNotFound();

	await ValidateCategoryAsync(model);
	if (!ModelState.IsValid)
	{
		await PopulateCategoryOptionsAsync(model);
		return View(model);
	}

	// Apply validated editable fields to the persisted Event.
	eventItem.Title = model.Title.Trim();
	eventItem.Description = model.Description.Trim();
	eventItem.CategoryId = model.CategoryId!.Value;
	eventItem.Location = model.Location.Trim();
	eventItem.StartDateTime = model.StartDateTime!.Value;
	eventItem.EndDateTime = model.EndDateTime!.Value;
	eventItem.Capacity = model.Capacity!.Value;

	await _context.SaveChangesAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was updated.";
	return RedirectToAction(nameof(Manage));
}

[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Edit(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	var eventItem = await _context.Events
		.AsNoTracking()
		.FirstOrDefaultAsync(item => item.Id == id && item.OrganizerId == organizer.Id);

	if (eventItem is null) return EventNotFound();

	if (eventItem.Status == EventStatus.Cancelled)
	{
		TempData["ErrorMessage"] = "A Cancelled Event can no longer be edited.";
		return RedirectToAction(nameof(Manage));
	}

	if (eventItem.Status != EventStatus.Draft && eventItem.Status != EventStatus.Published) return EventNotFound();

	var model = new EventInputModel
	{
		Title = eventItem.Title,
		Description = eventItem.Description,
		CategoryId = eventItem.CategoryId,
		Location = eventItem.Location,
		StartDateTime = eventItem.StartDateTime,
		EndDateTime = eventItem.EndDateTime,
		Capacity = eventItem.Capacity
	};

	await PopulateCategoryOptionsAsync(model);
	return View(model);
}
	[HttpGet]
	[Authorize(Roles = RoleNames.Organizer)]
	public async Task<IActionResult> Manage()
	{
		var organizer = await _userManager.GetUserAsync(User);

		if (organizer is null)
		{
			return Challenge();
		}

		var events = await _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem => eventItem.OrganizerId == organizer.Id)
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ThenBy(eventItem => eventItem.Id)
			.ToListAsync();

		return View(events);
	}

	[HttpPost]
	[Authorize(Roles = RoleNames.Organizer)]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(EventInputModel model)
	{
		await ValidateCategoryAsync(model);

		if (!ModelState.IsValid)
		{
			await PopulateCategoryOptionsAsync(model);
			return View(model);
		}

		var organizer = await _userManager.GetUserAsync(User);

		if (organizer is null)
		{
			return Challenge();
		}

		var eventItem = new Event
		{
			Title = model.Title.Trim(),
			Description = model.Description.Trim(),
			CategoryId = model.CategoryId!.Value,
			Location = model.Location.Trim(),
			StartDateTime = model.StartDateTime!.Value,
			EndDateTime = model.EndDateTime!.Value,
			Capacity = model.Capacity!.Value,

			OrganizerId = organizer.Id,
			Status = EventStatus.Draft
		};

		_context.Events.Add(eventItem);
		await _context.SaveChangesAsync();

		return RedirectToAction(nameof(Manage));
	}

	[HttpGet]
	[Authorize(Roles = RoleNames.Organizer)]
	public async Task<IActionResult> Create()
	{
		var model = new EventInputModel();

		await PopulateCategoryOptionsAsync(model);

		return View(model);
	}

	private async Task PopulateCategoryOptionsAsync(EventInputModel model)
	{
		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();

		model.CategoryOptions = categories
			.Select(category => new SelectListItem(category.Name, category.Id.ToString()))
			.ToList();
	}

	private async Task ValidateCategoryAsync(EventInputModel model)
	{
		if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0)
		{
			return;
		}

		var categoryExists = await _context.Categories
			.AnyAsync(category => category.Id == model.CategoryId.Value);

		if (!categoryExists)
		{
			ModelState.AddModelError(
				nameof(model.CategoryId),
				"Select an existing Category.");
		}
	}

	public async Task<IActionResult> Index(string? search, int? categoryId, int? page, int? pageSize)
	{
		_logger.LogInformation("Loading Published upcoming CEMS events with public filters and paging.");

		var pageSizeWasSupplied = Request.Query.ContainsKey(nameof(pageSize));

		if (pageSizeWasSupplied && (!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			var errorViewModel = new ErrorPageViewModel
			{
				Title = "Invalid Page Size",
				Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action("Index", "Events"),
				ReturnLabel = "Return to Events"
			};

			Response.StatusCode = StatusCodes.Status400BadRequest;

			return View("BadRequest", errorViewModel);
		}

		var currentPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var now = DateTime.Now;

		IQueryable<Event> query = _context.Events
			.AsNoTracking()
			.Include(eventItem => eventItem.Category)
			.Where(eventItem =>
				eventItem.Status == EventStatus.Published &&
				eventItem.StartDateTime > now);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var searchTerm = search.Trim();

			query = query.Where(eventItem =>
				eventItem.Title.Contains(searchTerm) ||
				eventItem.Location.Contains(searchTerm));
		}

		if (categoryId.HasValue)
		{
			query = query.Where(eventItem =>
				eventItem.CategoryId == categoryId.Value);
		}

		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

		var currentPage = page.GetValueOrDefault(1);

		if (currentPage < 1)
		{
			currentPage = 1;
		}

		if (totalPages > 0 && currentPage > totalPages)
		{
			currentPage = totalPages;
		}

		if (totalPages == 0)
		{
			currentPage = 1;
		}

		var events = await query
			.OrderBy(eventItem => eventItem.StartDateTime)
			.ThenBy(eventItem => eventItem.Id)
			.Skip((currentPage - 1) * currentPageSize)
			.Take(currentPageSize)
			.ToListAsync();

		var categories = await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync();

		var viewModel = new EventSearchViewModel
		{
			Events = events,
			Categories = categories,
			Search = search,
			CategoryId = categoryId,
			CurrentPage = currentPage,
			PageSize = currentPageSize,
			PageSizeOptions = AllowedPageSizes,
			TotalCount = totalCount,
			TotalPages = totalPages
		};

		return View(viewModel);
	}

[HttpGet]
public async Task<IActionResult> Details(int id)
{
	var eventItem = await _context.Events
		.AsNoTracking()
		.Include(item => item.Category)
		.Include(item => item.Organizer)
		.SingleOrDefaultAsync(item => item.Id == id &&
			(item.Status == EventStatus.Published || item.Status == EventStatus.Cancelled));

	if (eventItem is null) return  EventNotFound();

	return View(eventItem);
}
	private ViewResult EventNotFound()
	{
		Response.StatusCode = StatusCodes.Status404NotFound;
		return View("EventNotFound");
	}

}