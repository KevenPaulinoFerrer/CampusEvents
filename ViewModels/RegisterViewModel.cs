using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class RegisterViewModel
{
	[Required]
	[StringLength(100)]
	public string DisplayName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Compare(nameof(Password))]
	public string ConfirmPassword { get; set; } = string.Empty;
}
