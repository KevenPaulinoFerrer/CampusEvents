using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Services;

public enum RegistrationAttemptStatus
{
	Success,
	EventNotFound,
	Unavailable,
	Duplicate,
	Full
}

public class RegistrationService
{
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;

	public RegistrationService(ApplicationDbContext context, TimeProvider timeProvider)
	{
		_context = context;
		_timeProvider = timeProvider;
	}

	public async Task<RegistrationAttemptStatus> TryRegisterAsync(string userId, int eventId)
	{
		await using var transaction = await _context.Database.BeginTransactionAsync();

		var eventItem = await _context.Events.FromSqlInterpolated($@"
				SELECT *
				FROM [Events] WITH (UPDLOCK, HOLDLOCK)
				WHERE [Id] = {eventId}").SingleOrDefaultAsync();

		if (eventItem is null)
		{
			await transaction.RollbackAsync();
			return RegistrationAttemptStatus.EventNotFound;
		}

		var now = _timeProvider.GetLocalNow().DateTime;

		if (eventItem.Status != EventStatus.Published || eventItem.StartDateTime <= now)
		{
			await transaction.RollbackAsync();
			return RegistrationAttemptStatus.Unavailable;
		}

		var alreadyRegistered = await _context.Registrations.AnyAsync(registration => registration.UserId == userId &&
					registration.EventId == eventId);

		if (alreadyRegistered)
		{
			await transaction.RollbackAsync();
			return RegistrationAttemptStatus.Duplicate;
		}

		var currentRegistrationCount = await _context.Registrations.CountAsync(registration =>
					registration.EventId == eventId);

		if (currentRegistrationCount >= eventItem.Capacity)
		{
			await transaction.RollbackAsync();
			return RegistrationAttemptStatus.Full;
		}

		var registration = new Registration
		{
			UserId = userId, EventId = eventId, CreatedAt = now
		};

		_context.Registrations.Add(registration);

		try
		{
			await _context.SaveChangesAsync();
			await transaction.CommitAsync();

			return RegistrationAttemptStatus.Success;
		}
		catch (DbUpdateException exception) when (IsUniqueRegistrationViolation(exception))
		{
			await transaction.RollbackAsync();
			_context.ChangeTracker.Clear();

			return RegistrationAttemptStatus.Duplicate;
		}
	}

	private static bool IsUniqueRegistrationViolation(DbUpdateException exception)
	{
		return exception.InnerException is SqlException sqlException && (sqlException.Number == 2601 ||
				sqlException.Number == 2627);
	}
}
