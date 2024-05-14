namespace Application.Dtos.WeeklyScheduleDtos;

public class AvailableDayRequest
{
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; } = new TimeSpan(17, 0, 0);
	public TimeSpan SessionDuration { get; set; } = new TimeSpan(0, 30, 0);
	public TimeSpan EndTime { get; set; } = new TimeSpan(21, 0, 0);
}