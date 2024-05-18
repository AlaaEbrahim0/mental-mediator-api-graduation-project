namespace Application.Dtos.WeeklyScheduleDtos;

public class WeekDayResponse
{
	public DayOfWeek? DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
}


