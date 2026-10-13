using CampusEvents.Models;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CampusEvents.Controllers;

public class AccountController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;

	public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Register()
	{
		return View();
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Register(RegisterViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = new ApplicationUser
		{
			UserName = model.Email,
			Email = model.Email,
			DisplayName = model.DisplayName
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			return RedirectToAction("Index", "Events");
		}

		foreach (var error in result.Errors)
		{
			ModelState.AddModelError(string.Empty, error.Description);
		}

		return View(model);
	}

	[HttpGet]
	[AllowAnonymous]
	public IActionResult Login(string? returnUrl = null)
	{
		return View(new LoginViewModel
		{
			ReturnUrl = returnUrl
		});
	}

	[HttpPost]
	[AllowAnonymous]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var result = await _signInManager.PasswordSignInAsync(
			model.Email,
			model.Password,
			isPersistent: false,
			lockoutOnFailure: false);

		if (result.Succeeded)
		{
			if (Url.IsLocalUrl(model.ReturnUrl))
			{
				return LocalRedirect(model.ReturnUrl);
			}

			return RedirectToAction("Index", "Events");
		}

		ModelState.AddModelError(string.Empty, "Invalid login attempt.");

		return View(model);
	}
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Events");
    }
}
