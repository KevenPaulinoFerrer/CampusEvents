using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class EventInputModel : IValidatableObject
{
	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Select a Category.")]
	[Display(Name = "Category")]
	public int? CategoryId { get; set; }

	[Required]
	public string Location { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "Start date and time")]
	public DateTime? StartDateTime { get; set; }

	[Required]
	[DataType(DataType.DateTime)]
	[Display(Name = "End date and time")]
	public DateTime? EndDateTime { get; set; }

	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
	public int? Capacity { get; set; }

	public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (StartDateTime.HasValue && EndDateTime.HasValue && StartDateTime.Value >= EndDateTime.Value)
		{
			yield return new ValidationResult(
				"End date and time must be after the start date and time.",
				new[] { nameof(EndDateTime) });
		}
	}
}
