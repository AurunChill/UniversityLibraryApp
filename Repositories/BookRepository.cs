using LibraryApp.Data;
using LibraryApp.Data.Models;
using LibraryApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LibraryApp.Repositories;

public sealed class BookRepository : IBookRepository
{
    private readonly LibraryContext _ctx;
    public BookRepository(LibraryContext ctx) => _ctx = ctx;

    // ---------- базовый CRUD ----------

    public async Task<IReadOnlyList<Book>> GetAllAsync(
        int? limit = null,
        int offset = 0,
        Expression<Func<Book, bool>>? filter = null)
    {
        IQueryable<Book> q = _ctx.Books.AsNoTracking();
        if (filter is not null) q = q.Where(filter);
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);

        return await q.ToListAsync();
    }

    public Task<Book?> GetByIdAsync(long id) =>
        _ctx.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == id);

    public async Task<Book> AddAsync(Book entity)
    {
        _ctx.Books.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> UpdateAsync(Book entity)
    {
        _ctx.Books.Update(entity);
        return await _ctx.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var b = await _ctx.Books.FindAsync(id);
        if (b is null) return false;
        _ctx.Books.Remove(b);
        return await _ctx.SaveChangesAsync() > 0;
    }

    // ---------- расширенный поиск ----------

    public async Task<IReadOnlyList<Book>> SearchAsync(BookSearchArgs a)
    {
        IQueryable<Book> q = _ctx.Books.AsNoTracking();

        if (a.UseTitle && !string.IsNullOrWhiteSpace(a.Title))
            q = q.Where(b => EF.Functions.Like(b.Title, $"%{a.Title}%"));

        if (a.UseDescription && !string.IsNullOrWhiteSpace(a.Description))
            q = q.Where(b => EF.Functions.Like(b.Description!, $"%{a.Description}%"));

        if (!string.IsNullOrWhiteSpace(a.Author))
            q = q.Where(b => EF.Functions.Like(b.Author, $"%{a.Author}%"));

        if (a.PublishYear is not null)
            q = q.Where(b => b.PublishYear == a.PublishYear);

        if (!string.IsNullOrWhiteSpace(a.Publisher))
            q = q.Where(b => EF.Functions.Like(b.Publisher, $"%{a.Publisher}%"));

        if (a.Pages is not null)
            q = q.Where(b => b.Pages == a.Pages);

        if (!string.IsNullOrWhiteSpace(a.ISBN))
            q = q.Where(b => b.ISBN == a.ISBN);

        if (!string.IsNullOrWhiteSpace(a.Language))
            q = q.Where(b => b.Language == a.Language);

        if (!string.IsNullOrWhiteSpace(a.Genre))
            q = q.Where(b => b.Genre == a.Genre);

        return await q.ToListAsync();
    }

    public async Task<IReadOnlyList<Book>> FullTextSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<Book>();

        query = $"%{query}%";
        return await _ctx.Books.AsNoTracking()
            .Where(b =>
                   EF.Functions.Like(b.Title, query) ||
                   EF.Functions.Like(b.Description!, query) ||
                   EF.Functions.Like(b.Author, query))
            .ToListAsync();
    }
}
