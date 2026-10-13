using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class ParticipantController : Controller
{
	[Authorize]
	public IActionResult Index()
	{
		return View();
	}
}
