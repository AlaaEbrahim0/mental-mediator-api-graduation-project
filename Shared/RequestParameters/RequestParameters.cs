namespace Shared.RequestParameters;
public class RequestParameters
{
	private const int MaxPageSize = 50;
	public int PageNumber { get; set; } = 1;

	private int _pageSize = 20;
	public int PageSize
	{
		get => _pageSize;
		set
		{
			_pageSize = value > MaxPageSize ? MaxPageSize : value;
		}
	}
}