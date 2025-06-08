using Microsoft.EntityFrameworkCore;
namespace LibraryApp.Data.Services;

public class BaseService<T> where T : class
{
    protected readonly IDbContextFactory<LibraryContext> Factory;
    public BaseService(IDbContextFactory<LibraryContext> factory)
        => Factory = factory;

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<T> q = db.Set<T>().AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Set<T>().FindAsync(id);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Set<T>().Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Set<T>().Update(entity);
        return await db.SaveChangesAsync() > 0;
    }

    public virtual async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var entity = await db.Set<T>().FindAsync(id);
        if (entity is null) return false;
        db.Set<T>().Remove(entity);
        return await db.SaveChangesAsync() > 0;
    }
}
