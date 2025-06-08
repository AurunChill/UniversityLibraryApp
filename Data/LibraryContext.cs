using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data;

public class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<LanguageCode> LanguageCodes => Set<LanguageCode>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<GenreBook> GenreBooks => Set<GenreBook>();
    public DbSet<AuthorBook> AuthorBooks => Set<AuthorBook>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<ReaderTicket> ReaderTickets => Set<ReaderTicket>();
    public DbSet<Debt> Debts => Set<Debt>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Additional configuration if needed
    }
}
