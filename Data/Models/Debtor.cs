using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data;

[Index(nameof(ReaderTicketId), nameof(BookId), IsUnique = true)]
[Table("Debtor")]
public class Debtor
{
    [Key, Column("debtor_id")]
    public long DebtorId { get; set; }

    [Column("reader_ticket_id")]
    public long ReaderTicketId { get; set; }

    [Column("book_id")]
    public long BookId { get; set; }

    [Column("get_date")]
    public DateOnly GetDate { get; set; }

    [Column("return_date")]
    public DateOnly? ReturnDate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("debt_date")]
    public DateOnly DebtDate { get; set; }

    [Column("days_until_debt")]
    public int DaysUntilDebt { get; set; }

    [Column("late_penalty")]
    public double? LatePenalty { get; set; }

    public Book? Book { get; set; }
    public ReaderTicket? ReaderTicket { get; set; }
}

public static class Debtors
{
    public static IDbContextFactory<LibraryContext> Factory { get; private set; } = null!;
    public static void Init(IDbContextFactory<LibraryContext> f) => Factory = f;

    /* базовые */
    public static async Task<IReadOnlyList<Debtor>> GetAllAsync(int? limit = null, int off = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var q = db.Debtors
                  .Include(d => d.Book)
                  .Include(d => d.ReaderTicket)
                  .AsNoTracking()
                  .Skip(off);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public static async Task<Debtor?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debtors
                       .Include(d => d.Book)
                       .Include(d => d.ReaderTicket)
                       .AsNoTracking()
                       .FirstOrDefaultAsync(x => x.DebtorId == id);
    }

    public static async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var e = await db.Debtors.FindAsync(id);
        if (e is null) return false;
        db.Debtors.Remove(e);
        return await db.SaveChangesAsync() > 0;
    }

    public static async Task<Debtor> AddAsync(Debtor d)
    {
        d.DaysUntilDebt = CalcDays(d);

        await using var db = await Factory.CreateDbContextAsync();
        db.Debtors.Add(d);
        await db.SaveChangesAsync();

        var issueSupply = new Supply
        {
            BookId = d.BookId,
            Date = d.GetDate,
            OperationType = OperationType.Долг.Text(), // "долг"
            Amount = 1
        };
        await Supplies.AddAsync(issueSupply);

        return d;
    }

    public static async Task<bool> UpdateAsync(Debtor d)
    {
        d.DaysUntilDebt = CalcDays(d);

        await using var db = await Factory.CreateDbContextAsync();
        var original = await db.Debtors
                               .AsNoTracking()
                               .FirstOrDefaultAsync(x => x.DebtorId == d.DebtorId);

        bool wasReturned = original?.ReturnDate != null;
        bool nowReturned = d.ReturnDate != null;

        db.Debtors.Update(d);
        bool ok = await db.SaveChangesAsync() > 0;

        if (ok && !wasReturned && nowReturned)
        {
            var returnSupply = new Supply
            {
                BookId = d.BookId,
                Date = d.ReturnDate!.Value,
                OperationType = OperationType.Приход.Text(), 
                Amount = 1
            };
            await Supplies.AddAsync(returnSupply);
        }

        return ok;
    }

    private static int CalcDays(Debtor d)
    {
        var refDt = d.ReturnDate ?? DateOnly.FromDateTime(DateTime.Today);
        var span = refDt.ToDateTime(TimeOnly.MinValue) - d.DebtDate.ToDateTime(TimeOnly.MinValue);
        return Math.Abs(span.Days);
    }

    /* дополнительные запросы */
    public static async Task<IReadOnlyList<Debtor>> GetOpenDebtsAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debtors
                       .Include(d => d.Book)
                       .Include(d => d.ReaderTicket)
                       .Where(d =>
                           d.Status == DebtorStatus.Просрочено.Text() ||
                           d.Status == DebtorStatus.Возвращено_Без_Оплаты.Text() ||
                           d.Status == DebtorStatus.Утеряно.Text())
                       .AsNoTracking()
                       .ToListAsync();
    }

    public static async Task<int> DeleteClosedAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var list = db.Debtors.Where(d =>
                          d.Status == DebtorStatus.В_Срок.Text() ||
                          d.Status == DebtorStatus.Просрочено_Возвращено_С_Оплатой.Text());
        int cnt = await list.ExecuteDeleteAsync();
        return cnt;
    }

    public static async Task<IReadOnlyList<(long id, string title)>> GetDebtsForReaderAsync(long ticketId)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debtors
                       .Include(d => d.Book)
                       .Where(d =>
                           d.ReaderTicketId == ticketId &&
                           (d.Status == DebtorStatus.Просрочено.Text() || d.Status == DebtorStatus.В_Срок.Text()))
                       .Select(d => new ValueTuple<long, string>(d.BookId, d.Book!.Title))
                       .ToListAsync();
    }
}
