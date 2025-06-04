using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

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

    /// <summary>
    /// Возвращает все записи должников с навигационными свойствами Book и ReaderTicket.
    /// </summary>
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

    /// <summary>
    /// Получает одну запись должника по его ID.
    /// </summary>
    public static async Task<Debtor?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debtors
                       .Include(d => d.Book)
                       .Include(d => d.ReaderTicket)
                       .AsNoTracking()
                       .FirstOrDefaultAsync(x => x.DebtorId == id);
    }

    /// <summary>
    /// Удаляет запись должника по её ID.
    /// </summary>
    public static async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var e = await db.Debtors.FindAsync(id);
        if (e is null) return false;

        db.Debtors.Remove(e);
        return await db.SaveChangesAsync() > 0;
    }

    /// <summary>
    /// Добавляет новую запись должника.
    /// При выдаче книги автовычисляются DaysUntilDebt (по d.DebtDate) и LatePenalty = 0, 
    /// затем создаётся операция "долг" (Supply.OperationType = "долг") с Amount = 1.
    /// </summary>
    public static async Task<Debtor> AddAsync(Debtor d)
    {
        // 1. Рассчитываем начальное значение DaysUntilDebt
        d.DaysUntilDebt = CalcDays(d);
        // 2. При выдаче штраф пока 0
        d.LatePenalty = 0;

        await using var db = await Factory.CreateDbContextAsync();
        db.Debtors.Add(d);
        await db.SaveChangesAsync();

        // 3. Создаём операцию "долг" в журнале Supply
        var issueSupply = new Supply
        {
            BookId = d.BookId,
            Date = d.GetDate,
            OperationType = OperationType.Долг.Text(), // строка "долг"
            Amount = 1
        };
        await Supplies.AddAsync(issueSupply);

        return d;
    }

    /// <summary>
    /// Обновляет существующую запись должника.
    /// При возврате книги (если ReturnDate ранее был null, а теперь задан):
    ///  - пересчитывается DaysUntilDebt;
    ///  - если фактическая дата позже DebtDate, LatePenalty = DaysUntilDebt * 30;
    ///  - создаётся операция "приход" (Supply.OperationType = "приход") с Amount = 1;
    /// иначе LatePenalty = 0 и операции не создаётся.
    /// </summary>
    public static async Task<bool> UpdateAsync(Debtor d)
    {
        // 1. Пересчитываем DaysUntilDebt
        d.DaysUntilDebt = CalcDays(d);

        await using var db = await Factory.CreateDbContextAsync();

        // Получаем оригинальную запись (старую ReturnDate!)
        var original = await db.Debtors
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.DebtorId == d.DebtorId);

        bool wasReturned = original?.ReturnDate != null;
        bool nowReturned = d.ReturnDate != null;

        // 2. Рассчитываем штраф, если книга возвращена
        if (nowReturned)
        {
            // Штраф только если вернул ПОЗЖЕ, т.е. DaysUntilDebt > 0
            if (d.DaysUntilDebt > 0)
                d.LatePenalty = d.DaysUntilDebt * 30.0;
            else
                d.LatePenalty = 0;
        }
        else
        {
            d.LatePenalty = 0;
        }

        db.Debtors.Update(d);
        bool ok = await db.SaveChangesAsync() > 0;

        // 3. Если книга только сейчас возвращена — создаём поставку-приход
        if (ok && !wasReturned && nowReturned)
        {
            var returnSupply = new Supply
            {
                BookId        = d.BookId,
                Date          = d.ReturnDate!.Value,
                OperationType = OperationType.Приход.Text(),
                Amount        = 1
            };
            await Supplies.AddAsync(returnSupply);
        }

        return ok;
    }

    /// <summary>
    /// Подсчитывает разницу в днях между DebtDate и либо ReturnDate, либо сегодняшней датой (если ReturnDate == null).
    /// </summary>
        private static int CalcDays(Debtor d)
        {
            // days_until_debt = DebtDate - (ReturnDate или сегодня)
            var current = d.ReturnDate ?? DateOnly.FromDateTime(DateTime.Today);
            var days = d.DebtDate.ToDateTime(TimeOnly.MinValue) - current.ToDateTime(TimeOnly.MinValue);
            return days.Days;
        }


    /* дополнительные методы для получения только открытых долгов, удаления закрытых и т. д. */

    /// <summary>
    /// Возвращает список должников с открытыми долгами (статус «Просрочено», «Возвращено без оплаты» или «Утеряно»).
    /// </summary>
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

    /// <summary>
    /// Удаляет все записи должников со статусом «В срок» или «Просрочено возвращено с оплатой».
    /// </summary>
    public static async Task<int> DeleteClosedAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        var list = db.Debtors.Where(d =>
                          d.Status == DebtorStatus.В_Срок.Text() ||
                          d.Status == DebtorStatus.Просрочено_Возвращено_С_Оплатой.Text());
        int cnt = await list.ExecuteDeleteAsync();
        return cnt;
    }

    /// <summary>
    /// Возвращает кортеж (BookId, Title) для текущих долгов конкретного читателя.
    /// </summary>
    public static async Task<IReadOnlyList<(long id, string title)>> GetDebtsForReaderAsync(long ticketId)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Debtors
                       .Include(d => d.Book)
                       .Where(d =>
                           d.ReaderTicketId == ticketId &&
                           (d.Status == DebtorStatus.Просрочено.Text() || d.Status == DebtorStatus.В_Срок.Text()))
                       .Select(d => new { d.BookId, d.Book!.Title })
                       .ToListAsync()
                       .ContinueWith(t => (IReadOnlyList<(long id, string title)>)t.Result
                           .Select(x => (x.BookId, x.Title))
                           .ToList());
    }
}
