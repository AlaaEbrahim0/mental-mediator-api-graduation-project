namespace Application.Dtos.WeeklyScheduleDtos;

public class DoctorWeeklyScheduleResponse
{
	public string? DoctorId { get; set; }
	public List<WeekDayResponse> WeekDays { get; set; } = new();
}
