namespace Shared.RequestParameters;

public class MyAppointmentsRequestParameters : RequestParameters
{
	public string? ClientName { get; set; }
	public string? DoctorName { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public string? Status { get; set; }
}