namespace Shared.RequestParameters;

public class AppointmentRequestParameters : RequestParameters
{
	public string? DoctorId { get; set; }
	public string? UserId { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public string? Status { get; set; }
}