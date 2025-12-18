using System.Linq.Expressions;

namespace Domain.Contracts._Base
{
    public interface IRepositoryBase<T>
    {
        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

        Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

        Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> queryWhere, CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<T, bool>> queryWhere = null, CancellationToken cancellationToken = default);

        Task<T> GetFirstAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
            CancellationToken cancellationToken = default);

        Task<T> GetSingleAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
            CancellationToken cancellationToken = default);

        Task<List<T>> GetAllAsync(bool readOnly, Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null,
             Expression<Func<T, object>> orderBy = null, bool orderDescending = false, CancellationToken cancellationToken = default);

        Task<Tuple<int, List<T>>> GetAllPagedAsync(bool readOnly, int pageNumber, int pageSize, Expression<Func<T, object>> orderBy, bool orderDescending = false,
            Expression<Func<T, bool>> queryWhere = null, Expression<Func<T, object>>[] queryIncludes = null, CancellationToken cancellationToken = default);
    }
}
