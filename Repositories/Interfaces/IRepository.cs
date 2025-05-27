using System.Linq.Expressions;

namespace LibraryApp.Repositories;

public interface IRepository<TEntity, in TKey>
    where TEntity : class
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        int? limit = null,
        int offset = 0,
        Expression<Func<TEntity, bool>>? filter = null);

    Task<TEntity?> GetByIdAsync(TKey id);

    Task<TEntity> AddAsync(TEntity entity);
    Task<bool> UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(TKey id);
}
