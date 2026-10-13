namespace CampusEvents.ViewModels;

public class ErrorPageViewModel
{
    public string Title { get; init; } = "Invalid Request";

    public string Message { get; init; } = "The request could not be processed.";

    public string? ReturnUrl { get; init; }

    public string ReturnLabel { get; init; } = "Return";
}