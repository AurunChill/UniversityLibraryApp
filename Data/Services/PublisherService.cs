using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class PublisherService : BaseService<Publisher>
{
    public PublisherService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Publisher>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Publisher> query = db.Publishers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => p.Name.Contains(q));
        }
        return await query.ToListAsync();
    }
}
