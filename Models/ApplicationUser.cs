using Microsoft.AspNetCore.Identity;

namespace CampusEvents.Models;

public class ApplicationUser : IdentityUser
{
	public string DisplayName { get; set; } = string.Empty;
	public ICollection<Registration> Registrations { get; set; } = new List<Registration>();


	public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}

