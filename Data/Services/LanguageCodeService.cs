using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class LanguageCodeService : BaseService<LanguageCode>
{
    public LanguageCodeService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<LanguageCode>> FullTextAsync(string q)
    {
        await using var db = await Factory.CreateDbContextAsync();
        IQueryable<LanguageCode> query = db.LanguageCodes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(l => l.Code.Contains(q));
        }
        return await query.ToListAsync();
    }
}
