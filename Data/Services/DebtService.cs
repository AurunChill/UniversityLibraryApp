using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class DebtService : BaseService<Debt>
{
    public DebtService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Debt>> GetOpenDebtsAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debts
            .Include(d => d.Book)
            .Include(d => d.ReaderTicket)
            .Where(d => d.EndTime == null)
            .AsNoTracking()
            .ToListAsync();
    }
}
