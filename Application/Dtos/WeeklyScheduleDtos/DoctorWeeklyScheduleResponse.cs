namespace Application.Dtos.WeeklyScheduleDtos;

public class DoctorWeeklyScheduleResponse
{
	public string DoctorId { get; set; } = string.Empty;
	public List<CreateScheduleWeekDayRequest> WeekDays { get; set; } = new();
}
