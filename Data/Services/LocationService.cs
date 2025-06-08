using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data.Services;

public class LocationService : BaseService<Location>
{
    public LocationService(IDbContextFactory<LibraryContext> factory) : base(factory) { }

    public async Task<IReadOnlyList<Location>> GetNonEmptyAsync()
    {
        await using var db = await Factory.CreateDbContextAsync();
        return await db.Locations
            .Where(l => l.Amount > 0)
            .AsNoTracking()
            .ToListAsync();
    }
}
