using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(BookId), nameof(Date))]
[Table("Supply")]
public class Supply
{
    [Key, Column("supply_id")]
    public long SupplyId { get; set; }
    [Column("book_id")]
    public long BookId { get; set; }
    [Column("date")]
    public DateOnly Date { get; set; }
    [Column("operation_type")]
    public required string OperationType { get; set; }
    [Column("amount")]
    public int Amount { get; set; }
    public Book? Book { get; set; }
}

public static class Supplies
{
    public static IDbContextFactory<LibraryContext> Factory { get; set; } = null!;

    public static async Task<IReadOnlyList<Supply>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Supply> q = db.Supplies
                                 .Include(s => s.Book)
                                 .OrderByDescending(s => s.Date)
                                 .AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public static async Task<Supply?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Supplies.Include(s => s.Book)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(s => s.SupplyId == id);
    }

    public static async Task<IReadOnlyList<Supply>> GetHistoryForBookAsync(
        long bookId, int limit = 50, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<Supply> q = db.Supplies.Where(s => s.BookId == bookId)
                                          .Include(s => s.Book)
                                          .OrderByDescending(s => s.Date)
                                          .AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        q = q.Take(limit);
        return await q.ToListAsync();
    }

    public static async Task<Supply> AddAsync(Supply s)
    {
        await using var db = await Factory.CreateDbContextAsync();

        var book = await db.Books.FirstOrDefaultAsync(b => b.BookId == s.BookId)
                   ?? throw new ArgumentException("Книга не найдена");

        /* проверка: списываем/берём в долг больше, чем имеем */
        if (s.Amount < 0 && Math.Abs(s.Amount) > book.Amount)
            throw new InvalidOperationException("Недостаточно экземпляров книги");

        db.Supplies.Add(s);

        book.Amount += s.Amount;           // приход → +, списание/долг → -
        if (book.Amount < 0) book.Amount = 0;

        await db.SaveChangesAsync();
        return s;
    }

    public static async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var entity = await db.Supplies.FindAsync(id);
        if (entity is null) return false;
        db.Supplies.Remove(entity);
        return await db.SaveChangesAsync() > 0;
    }
}