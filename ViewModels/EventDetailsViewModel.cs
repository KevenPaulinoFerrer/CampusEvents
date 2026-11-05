using CampusEvents.Models;

namespace CampusEvents.ViewModels;

public class EventDetailsViewModel
{
	public Event EventItem { get; set; } = null!;

	public int CurrentRegistrationCount { get; set; }

	public int RemainingCapacity { get; set; }

	public bool IsCurrentUserRegistered { get; set; }
}
