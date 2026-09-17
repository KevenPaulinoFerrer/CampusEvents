namespace CampusEvents.Models;

public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
