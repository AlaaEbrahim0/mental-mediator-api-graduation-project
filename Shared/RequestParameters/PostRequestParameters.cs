namespace Shared.RequestParameters;

public class PostRequestParameters : RequestParameters
{
	public string? Title { get; set; } = string.Empty;
	public string? Content { get; set; } = string.Empty;
	public string? Username { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public bool ConfessionsOnly { get; set; }
}