namespace Application.Dtos.WeeklyScheduleDtos;

public class AvailableDayResponse
{
	public int Id { get; set; }
	public DayOfWeek? DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
}


