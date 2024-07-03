using Domain.Enums;

namespace Domain.Value_Objects;

public class AppointmentStatusCount
{
	public AppointmentStatus Status { get; set; }
	public int Count { get; set; }
}
