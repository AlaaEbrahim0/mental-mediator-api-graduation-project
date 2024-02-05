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
