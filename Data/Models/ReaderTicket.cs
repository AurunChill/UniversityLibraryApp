using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data;

[Index(nameof(Email), IsUnique = true)]
[Table("ReaderTicket")]
public class ReaderTicket
{
  [Key, Column("reader_ticket_id")]
  public long ReaderTicketId { get; set; }
  [Column("full_name")]
  public string FullName { get; set; } = null!;
  [Column("email")]
  public string Email { get; set; } = null!;
  [Column("phone_number")]
  public string PhoneNumber { get; set; } = null!;
  [Column("extra_phone_number")]
  public string? ExtraPhoneNumber { get; set; }
  public ICollection<Debtor>? Debtors { get; set; }
}

public static class ReaderTickets
{
  public static IDbContextFactory<LibraryContext> Factory { get; private set; } = null!;
  public static void Init(IDbContextFactory<LibraryContext> f) => Factory = f;

  public static async Task<IReadOnlyList<ReaderTicket>> FullTextAsync(string q)
  {
    if (string.IsNullOrWhiteSpace(q))
      return await GetAllAsync(null, 0);

    q = q.Trim();

    await using var db = await Factory.CreateDbContextAsync();
    var list = await db.ReaderTickets.AsNoTracking().ToListAsync();

    return list.Where(r =>
            r.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(r.Email) && r.Email.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(r.PhoneNumber) && r.PhoneNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(r.ExtraPhoneNumber) && r.ExtraPhoneNumber.Contains(q, StringComparison.OrdinalIgnoreCase)))
        .ToList();
  }

  public static async Task<IReadOnlyList<ReaderTicket>> GetAllAsync(int? limit = null, int off = 0)
  {
    await using var db = await Factory.CreateDbContextAsync();
    var q = db.ReaderTickets.AsNoTracking().Skip(off);
    if (limit is not null) q = q.Take(limit.Value);
    return await q.ToListAsync();
  }

  public static async Task<ReaderTicket?> GetByIdAsync(long id)
  {
    await using var db = await Factory.CreateDbContextAsync();
    return await db.ReaderTickets.AsNoTracking().FirstOrDefaultAsync(x => x.ReaderTicketId == id);
  }

  public static async Task<ReaderTicket> AddAsync(ReaderTicket rt)
  {
    await using var db = await Factory.CreateDbContextAsync();
    db.ReaderTickets.Add(rt); await db.SaveChangesAsync(); return rt;
  }

  public static async Task<bool> UpdateAsync(ReaderTicket rt)
  {
    await using var db = await Factory.CreateDbContextAsync();
    db.ReaderTickets.Update(rt); return await db.SaveChangesAsync() > 0;
  }

  public static async Task<bool> DeleteAsync(long id)
  {
    await using var db = await Factory.CreateDbContextAsync();
    var e = await db.ReaderTickets.FindAsync(id); if (e is null) return false;
    db.ReaderTickets.Remove(e); return await db.SaveChangesAsync() > 0;
  }
}
