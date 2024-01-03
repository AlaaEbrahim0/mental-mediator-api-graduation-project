using System.Linq.Expressions;

namespace Infrastructure.Contracts;
public interface IRepositoryBase<T>
{
    IQueryable<T> FindAll(bool trackChanges);
    IQueryable<T?> FindByCondition(Expression<Func<T, bool>> conition, bool trackChanges);
    void Create(T entity);
    void Update(T entity);
    void Delete(T entity);
}
