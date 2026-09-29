using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;
 using CampusEvents.Models;	
 using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException(
		"Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	await DbInitializer.SeedAsync(context);
	var academicId = await context.Categories
	.Where(category => category.Name == "Academic")
	.Select(category => category.Id)
	.SingleAsync();

	if (!await context.Events.AnyAsync(eventItem =>
		eventItem.Title == "Pagination Practice Workshop 01"))
	{
		var createdAt = DateTime.Now;
		var practiceEvents = new List<Event>();

		await context.SaveChangesAsync();
	}

}

app.Run();
