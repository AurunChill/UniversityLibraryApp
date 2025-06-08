using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class ReaderTicketService : BaseService<ReaderTicket>
{
    public ReaderTicketService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

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
}
