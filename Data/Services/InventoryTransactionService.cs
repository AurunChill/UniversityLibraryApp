using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class InventoryTransactionService : BaseService<InventoryTransaction>
{
    public InventoryTransactionService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public override async Task<IReadOnlyList<InventoryTransaction>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<InventoryTransaction> q = db.InventoryTransactions
            .Include(t => t.Location)
            .Include(t => t.PrevLocation)
            .Include(t => t.Book)
            .AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }


    public async Task<IReadOnlyList<InventoryTransaction>> GetForBookAsync(long bookId)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.InventoryTransactions
            .Include(t => t.Location)
            .Include(t => t.PrevLocation)
            .Where(t => t.BookId == bookId)
            .AsNoTracking()
            .ToListAsync();
    }
}
