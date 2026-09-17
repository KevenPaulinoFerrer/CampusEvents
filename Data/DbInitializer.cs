using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
	private const string OrganizerId = "seed-organizer-001";

	public static async Task SeedAsync(ApplicationDbContext context)
	{
		if (await context.Events.AnyAsync())
		{
			return;
		}

		var organizer = new ApplicationUser
		{
			Id = OrganizerId,
			DisplayName = "Campus Events Office"
		};

		var academic = new Category
		{
			Name = "Academic"
		};

		var social = new Category
		{
			Name = "Social"
		};

		var career = new Category
		{
			Name = "Career"
		};

		context.Users.Add(organizer);
		context.Categories.AddRange(academic, social, career);

		await context.SaveChangesAsync();

		var createdAt = DateTime.Now;

		var libraryStart = DateTime.Today.AddDays(3).AddHours(18);
		var careerStart = DateTime.Today.AddDays(5).AddHours(12);
		var draftStart = DateTime.Today.AddDays(7).AddHours(15);
		var cancelledStart = DateTime.Today.AddDays(9).AddHours(20);
		var pastStart = DateTime.Today.AddDays(-10).AddHours(13);

		var events = new[]
		{
			new Event
			{
				Title = "Library Research Night",
				Description = "An evening workshop on research databases and source evaluation.",
				Location = "Main Library, Room 204",
				StartDateTime = libraryStart,
				EndDateTime = libraryStart.AddHours(2),
				Capacity = 60,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = academic.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Career Networking Lunch",
				Description = "Meet local employers and practice short professional introductions.",
				Location = "Student Center Ballroom",
				StartDateTime = careerStart,
				EndDateTime = careerStart.AddHours(2),
				Capacity = 120,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Welcome Mixer Planning Draft",
				Description = "A draft event used to demonstrate that Draft Events are not public.",
				Location = "Student Center Patio",
				StartDateTime = draftStart,
				EndDateTime = draftStart.AddHours(2),
				Capacity = 80,
				Status = EventStatus.Draft,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Outdoor Movie Night",
				Description = "A cancelled event retained in the database for lifecycle examples.",
				Location = "Campus Green",
				StartDateTime = cancelledStart,
				EndDateTime = cancelledStart.AddHours(3),
				Capacity = 200,
				Status = EventStatus.Cancelled,
				CreatedAt = createdAt,
				CategoryId = social.Id,
				OrganizerId = organizer.Id
			},
			new Event
			{
				Title = "Alumni Panel Archive",
				Description = "A past Published event used to demonstrate upcoming-event filtering.",
				Location = "Business Building Auditorium",
				StartDateTime = pastStart,
				EndDateTime = pastStart.AddHours(2),
				Capacity = 100,
				Status = EventStatus.Published,
				CreatedAt = createdAt,
				CategoryId = career.Id,
				OrganizerId = organizer.Id
			}
		};

		context.Events.AddRange(events);

		await context.SaveChangesAsync();
	}
}
