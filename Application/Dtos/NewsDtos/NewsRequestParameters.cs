using System.ComponentModel;

namespace Application.Dtos.NewsDtos;

public class NewsRequestParameters
{
	[DefaultValue("psychology OR Neuroscience OR Therapy OR Mental health OR Medical research OR Clinical psychology")]
	public string Query { get; set; } = "psychology OR Neuroscience OR Therapy OR Mental health OR Medical research OR Clinical psychology";
	public int PageSize { get; set; } = 50;
	public int Page { get; set; } = 1;
	[DefaultValue("en")]
	public string Language { get; set; } = "en";
}