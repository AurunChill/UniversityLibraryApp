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
        var all = await GetAllDetailedAsync();

        if (string.IsNullOrWhiteSpace(q))
            return all;

        return all.Where(b =>
                b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(b.Description) &&
                     b.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                b.Authors.Any(a => a.Author!.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (b.Publisher != null &&
                     b.Publisher.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            )
            .ToList();
    }
}
