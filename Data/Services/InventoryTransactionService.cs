using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class InventoryTransactionService : BaseService<InventoryTransaction>
{
    public InventoryTransactionService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

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
