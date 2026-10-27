namespace Domain.Repositories;

/// <summary>
/// A transaction-scoped lock for one doctor's bookable time. Booking and
/// scheduling mutations acquire it before they validate availability, so two
/// concurrent requests cannot both observe the same free slot and insert.
/// </summary>
public interface IAppointmentBookingLock : IAsyncDisposable
{
	Task CommitAsync(CancellationToken cancellationToken = default);
}
