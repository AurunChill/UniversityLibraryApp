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
    public DbSet<BookLanguage> BookLanguages => Set<BookLanguage>();
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<ReaderTicket> ReaderTickets => Set<ReaderTicket>();
    public DbSet<Debt> Debts => Set<Debt>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InventoryTransaction>()
            .HasOne(t => t.Location)
            .WithMany(l => l.Transactions)
            .HasForeignKey(t => t.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<InventoryTransaction>()
            .HasOne(t => t.PrevLocation)
            .WithMany()
            .HasForeignKey(t => t.PrevLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Book>()
            .HasOne(bk => bk.Language)
            .WithMany(l => l.Books)
            .HasForeignKey(bk => bk.LangId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BookLanguage>()
            .HasOne(bl => bl.Book)
            .WithMany(bk => bk.Languages)
            .HasForeignKey(bl => bl.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BookLanguage>()
            .HasOne(bl => bl.Language)
            .WithMany()
            .HasForeignKey(bl => bl.LangId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
