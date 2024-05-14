namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateAvailableDayRequest : UpdateAvailableDayRequest
{
	public DayOfWeek DayOfWeek { get; set; }
}
