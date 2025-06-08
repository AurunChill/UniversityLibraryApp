using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class BookService : BaseService<Book>
{
    public BookService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Book>> GetAllDetailedAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.Authors).ThenInclude(ab => ab.Author)
            .Include(b => b.Genres).ThenInclude(gb => gb.Genre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Book>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Book> query = db.Books
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.Authors).ThenInclude(ab => ab.Author)
            .Include(b => b.Genres).ThenInclude(gb => gb.Genre)
            .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(q) ||
                (b.Description != null && b.Description.ToLower().Contains(q)) ||
                b.Authors.Any(a => a.Author!.Name.ToLower().Contains(q)) ||
                (b.Publisher != null && b.Publisher.Name.ToLower().Contains(q))
            );
        }
        return await query.ToListAsync();
    }
}
