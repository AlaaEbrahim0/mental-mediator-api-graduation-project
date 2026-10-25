namespace Domain.Enums;

/// <summary>
/// How a doctor schedule block repeats. A block with a recurrence marks time
/// the doctor is unavailable, either as a one-off period or as a recurring
/// break inside the normal working hours.
/// </summary>
public enum ScheduleBlockRecurrence
{
	/// <summary>Blocks the exact StartTime..EndTime range once (vacation, one-off errand).</summary>
	None = 0,

	/// <summary>Repeats the same time-of-day window every day between StartTime and RecurrenceEndDate.</summary>
	Daily = 1,

	/// <summary>Repeats the same weekday and time-of-day window every week between StartTime and RecurrenceEndDate.</summary>
	Weekly = 2
}