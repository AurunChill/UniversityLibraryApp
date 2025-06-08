using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class DebtService : BaseService<Debt>
{
    public DebtService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public override async Task<IReadOnlyList<Debt>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Debt> q = db.Debts
            .Include(d => d.Book)
            .Include(d => d.ReaderTicket).ThenInclude(t => t.Reader)
            .AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public override async Task<Debt?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debts
            .Include(d => d.Book)
            .Include(d => d.ReaderTicket).ThenInclude(t => t.Reader)
            .FirstOrDefaultAsync(d => d.DebtId == id);
    }


    public async Task<IReadOnlyList<Debt>> GetOpenDebtsAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debts
            .Include(d => d.Book)
            .Include(d => d.ReaderTicket).ThenInclude(t => t.Reader)
            .Where(d => d.EndTime == null)
            .AsNoTracking()
            .ToListAsync();
    }
}
