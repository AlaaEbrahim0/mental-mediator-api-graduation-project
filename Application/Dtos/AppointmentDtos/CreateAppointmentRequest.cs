
namespace Application.Dtos.AppointmentDtos;
public class CreateAppointmentRequest
{
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }
	public string? Location { get; set; }
	public string? Reason { get; set; }
	public decimal Fees { get; set; }
}
