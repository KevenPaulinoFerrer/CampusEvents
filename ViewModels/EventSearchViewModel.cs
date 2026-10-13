using CampusEvents.Models;
using System;
using System.Collections.Generic;

namespace CampusEvents.ViewModels;

public class EventSearchViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();

	public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

	public string? Search { get; set; }

	public int? CategoryId { get; set; }

	public int CurrentPage { get; set; } = 1;

	public int PageSize { get; set; }

	public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

	public int TotalCount { get; set; }

	public int TotalPages { get; set; }
}
