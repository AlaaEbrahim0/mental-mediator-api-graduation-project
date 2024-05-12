namespace Application.Contracts;
public interface IDoctorWeeklyScheduleService
{

}

public class DoctorWeeklyScheduleResponse
{
	public int Id { get; set; }
	public string DoctorId { get; set; }
	public string DoctorName { get; set; }
	public Dictionary<DayOfWeek, AvailableDayResponse> Schedule = new();
}

public class CreateDoctorWeeklyScheduleRequest
{
}

public class AvailableDayResponse
{
	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
	public string Location { get; set; } = string.Empty;
}
