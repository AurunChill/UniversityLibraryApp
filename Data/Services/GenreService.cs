using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class GenreService : BaseService<Genre>
{
    public GenreService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Genre>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Genre> query = db.Genres.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(g => g.Name.Contains(q));
        }
        return await query.ToListAsync();
    }
}
