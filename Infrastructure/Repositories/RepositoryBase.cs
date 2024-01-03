using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Contracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    private readonly AppDbContext _dbContext;

    public RepositoryBase(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Create(T entity)
    {
        _dbContext.Set<T>().Add(entity);
    }

    public void Delete(T entity)
    {
        _dbContext.Set<T>().Remove(entity);

    }

    public IQueryable<T> FindAll(bool trackChanges)
    {
        return
            !trackChanges ?
            _dbContext.Set<T>() :
            _dbContext.Set<T>().AsNoTracking();
    }

    public IQueryable<T?> FindByCondition(Expression<Func<T, bool>> conition, bool trackChanges)
    {
        return
            !trackChanges ?
            _dbContext.Set<T>().Where(conition) :
            _dbContext.Set<T>().Where(conition).AsNoTracking();
    }

    public void Update(T entity)
    {
        _dbContext.Set<T>().Update(entity);
    }
}
