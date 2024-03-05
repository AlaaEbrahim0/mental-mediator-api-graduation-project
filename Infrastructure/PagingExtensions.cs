namespace Infrastructure;

public static class RepositoryPagingExtensions
{
    public static IQueryable<TSource> Paginate<TSource>(this IQueryable<TSource> source, int page, int pageSize)
    {
        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }
}