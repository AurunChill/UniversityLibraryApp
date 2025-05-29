using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ISBN), IsUnique = true)]
[Table("Books")]
public class Book
{
    [Key]
    [Column("book_id")]
    public long BookId { get; set; }

    [Required, Column("ISBN")]
    public string ISBN { get; set; } = null!;

    [Required, Column("language")]
    public string Language { get; set; } = null!;

    [Required, Column("location")]
    public string Location { get; set; } = null!;

    [Required, Column("title")]
    public string Title { get; set; } = null!;

    [Required, Column("author")]
    public string Author { get; set; } = null!;

    [Required, Column("genre")]
    public string Genre { get; set; } = null!;

    [Column("publish_year")]
    public int PublishYear { get; set; }

    [Required, Column("publisher")]
    public string Publisher { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Required, Column("cover_url")]
    public string CoverUrl { get; set; } = null!;

    [Column("pages")]
    public int Pages { get; set; }

    [Column("amount")]
    public int Amount { get; set; }

    /* навигации */
    public ICollection<Supply>? Supplies { get; set; }
    public ICollection<Debtor>? Debtors { get; set; }
}

public static class Books
{
    public static IDbContextFactory<LibraryContext> Factory { get; set; } = null!;

    public static async Task<IReadOnlyList<Book>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Book> q = db.Books.AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public static async Task<IReadOnlyList<Book>> FullTextAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return await GetAllAsync(null, 0);
        q = $"%{q}%";

        await using var db = await Factory.CreateDbContextAsync();
        return await db.Books.AsNoTracking()
                  .Where(b => EF.Functions.Like(b.Title, q) ||
                              EF.Functions.Like(b.Description!, q) ||
                              EF.Functions.Like(b.Author, q))
                  .ToListAsync();
    }

    public static async Task<Book> AddAsync(Book b)
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Books.Add(b);
        await db.SaveChangesAsync();
        return b;
    }

    public static async Task<bool> UpdateAsync(Book b)
    {
        await using var db = await Factory.CreateDbContextAsync();
        db.Books.Update(b);
        return await db.SaveChangesAsync() > 0;
    }

    public static async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var e = await db.Books.FindAsync(id);
        if (e is null) return false;
        db.Books.Remove(e);
        return await db.SaveChangesAsync() > 0;
    }
}