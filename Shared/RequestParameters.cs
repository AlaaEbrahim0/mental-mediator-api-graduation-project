namespace Shared;
public class RequestParameters
{
	private const int maxPageSize = 50;
	public int PageNumber { get; set; } = 1;

	private int pageSize = 20;
	public int PageSize
	{
		get
		{
			return pageSize;
		}
		set
		{
			pageSize = (value > maxPageSize) ? maxPageSize : value;
		}
	}
}

public class PostRequestParameters : RequestParameters
{
	public string? Title { get; set; } = string.Empty;
	public string? Content { get; set; } = string.Empty;
	public string? Username { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public bool ConfessionsOnly { get; set; }
}
public class DoctorRequestParameters : RequestParameters
{
	public string? Name { get; set; }
	public string? Specialization { get; set; } = string.Empty;
	public string? Gender { get; set; }
	public string? City { get; set; }
	public decimal MinFees { get; set; }
	public decimal MaxFees { get; set; }
}
public class AppointmentRequestParameters : RequestParameters
{
	public string? DoctorId { get; set; }
	public string? UserId { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public string? Status { get; set; }
}
