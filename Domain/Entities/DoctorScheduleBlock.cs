using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A period during which a doctor is not available for appointments.
///
/// One entity covers the two cases the scheduling flow needs:
/// <list type="bullet">
/// <item><see cref="ScheduleBlockRecurrence.None"/> blocks one specific
/// StartTime..EndTime range (vacation, errand, external commitment).</item>
/// <item><see cref="ScheduleBlockRecurrence.Daily"/> / <c>Weekly</c> repeat the
/// same time-of-day window (lunch break, fixed weekday off) until
/// <see cref="RecurrenceEndDate"/>.</item>
/// </list>
///
/// Blocks are advisory for the slot calculator and booking validation: an
/// appointment that overlaps a block can still exist (for example one booked
/// before the doctor added the block), so blocks never modify appointments by
/// themselves. Conflict reporting is handled in the services.
/// </summary>
public class DoctorScheduleBlock
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public Doctor Doctor { get; set; } = null!;

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public ScheduleBlockRecurrence Recurrence { get; set; } = ScheduleBlockRecurrence.None;

	/// <summary>Inclusive end date for recurring blocks. Ignored for one-off blocks.</summary>
	public DateTime? RecurrenceEndDate { get; set; }

	public string Reason { get; set; } = string.Empty;
}