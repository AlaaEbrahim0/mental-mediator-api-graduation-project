namespace Shared.RequestParameters;

public class PostRequestParameters : RequestParameters
{
	public string? Title { get; set; } = string.Empty;
	public string? Content { get; set; } = string.Empty;
	public string? Username { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public bool ConfessionsOnly { get; set; }

	/// <summary>
	/// Set by the service from the caller's role; never trust the query string
	/// value bound into this property.
	/// </summary>
	public bool IncludeDoctorsOnly { get; set; }
}