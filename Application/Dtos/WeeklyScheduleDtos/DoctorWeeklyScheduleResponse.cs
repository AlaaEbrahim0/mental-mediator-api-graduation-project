namespace Application.Dtos.WeeklyScheduleDtos;

public class DoctorWeeklyScheduleResponse
{
	public int Id { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public List<AvailableDayResponse> AvailableDays { get; set; } = new();
}
