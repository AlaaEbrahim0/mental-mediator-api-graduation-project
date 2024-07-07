namespace Shared.RequestParameters;

public class DoctorRequestParameters : RequestParameters
{
	public string? Name { get; set; }
	public string? Specialization { get; set; } = string.Empty;
	public string? Gender { get; set; }
	public string? City { get; set; }
	public decimal MinFees { get; set; }
	public decimal MaxFees { get; set; }
}