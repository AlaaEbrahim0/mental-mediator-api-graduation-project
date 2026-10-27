using System.Data;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

/// <summary>
/// Serializes booking and schedule mutations for one doctor with a SQL Server
/// transaction-scoped application lock. The lock is released when the
/// transaction commits or rolls back.
/// </summary>
internal sealed class SqlServerAppointmentBookingLock : IAppointmentBookingLock
{
	private readonly IDbContextTransaction _transaction;
	private bool _committed;

	public SqlServerAppointmentBookingLock(IDbContextTransaction transaction)
	{
		_transaction = transaction;
	}

	public async Task CommitAsync(CancellationToken cancellationToken = default)
	{
		await _transaction.CommitAsync(cancellationToken);
		_committed = true;
	}

	public async ValueTask DisposeAsync()
	{
		if (!_committed)
		{
			await _transaction.RollbackAsync();
		}

		await _transaction.DisposeAsync();
	}

	public static async Task<IAppointmentBookingLock> AcquireAsync(AppDbContext dbContext, string doctorId)
	{
		var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

		try
		{
			// SQL Server has no exclusion constraint for time ranges. An
			// application lock keyed by doctor is transactional, so it is
			// released automatically if the request fails or the process dies.
			await dbContext.Database.ExecuteSqlRawAsync(
				"""
				DECLARE @result int;
				EXEC @result = sys.sp_getapplock
					@Resource = {0},
					@LockMode = 'Exclusive',
					@LockOwner = 'Transaction',
					@LockTimeout = 10000;

				IF @result < 0
				BEGIN
					;THROW 51000, 'Could not acquire the doctor booking lock.', 1;
				END;
				""",
				$"doctor-booking:{doctorId}");

			return new SqlServerAppointmentBookingLock(transaction);
		}
		catch
		{
			await transaction.RollbackAsync();
			await transaction.DisposeAsync();
			throw;
		}
	}
}

/// <summary>
/// Test/provider fallback. Production uses SQL Server, where the real lock is
/// selected by <see cref="AppointmentRepository"/>.
/// </summary>
internal sealed class NoOpAppointmentBookingLock : IAppointmentBookingLock
{
	public static readonly NoOpAppointmentBookingLock Instance = new();

	public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
