using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class ReaderService : BaseService<Reader>
{
    public ReaderService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Reader>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Reader> query = db.Readers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(r => r.FullName.Contains(q) || r.Email.Contains(q));
        }
        return await query.ToListAsync();
    }
}
