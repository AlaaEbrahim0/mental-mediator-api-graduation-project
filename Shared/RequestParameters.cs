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

public static class PagingExtensions
{
    //used by LINQ to SQL
    public static IQueryable<TSource> Paginate<TSource>(this IQueryable<TSource> source, int page, int pageSize)
    {
        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }

    //used by LINQ
    public static IEnumerable<TSource> Paginate<TSource>(this IEnumerable<TSource> source, int page, int pageSize)
    {
        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }

}