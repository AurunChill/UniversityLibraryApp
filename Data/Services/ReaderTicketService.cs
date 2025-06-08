using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class ReaderTicketService : BaseService<ReaderTicket>
{
    public ReaderTicketService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public override async Task<IReadOnlyList<ReaderTicket>> GetAllAsync(int? limit = null, int offset = 0)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<ReaderTicket> q = db.ReaderTickets
            .Include(t => t.Reader)
            .AsNoTracking();
        if (offset > 0) q = q.Skip(offset);
        if (limit is not null) q = q.Take(limit.Value);
        return await q.ToListAsync();
    }

    public override async Task<ReaderTicket?> GetByIdAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.ReaderTickets
            .Include(t => t.Reader)
            .FirstOrDefaultAsync(t => t.ReaderId == id);
    }


    public async Task<IReadOnlyList<ReaderTicket>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<ReaderTicket> query = db.ReaderTickets
            .Include(t => t.Reader)
            .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(t => t.Reader!.FullName.Contains(q) || t.Reader.Email.Contains(q));
        }
        return await query.ToListAsync();
    }

    public override async Task<bool> DeleteAsync(long id)
    {
        await using var db = await Factory.CreateDbContextAsync();
        var ticket = await db.ReaderTickets.FindAsync(id);
        if (ticket is null) return false;
        db.ReaderTickets.Remove(ticket);
        var reader = await db.Readers.FindAsync(id);
        if (reader is not null) db.Readers.Remove(reader);
        return await db.SaveChangesAsync() > 0;
    }
}
