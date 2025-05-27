using Microsoft.EntityFrameworkCore;
using LibraryApp.Data.Models;

namespace LibraryApp.Data;

public class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<ReaderTicket> ReaderTickets => Set<ReaderTicket>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Пример: enum-to-string (EF хранит enum как TEXT)
        b.Entity<Supply>()
         .Property(s => s.OperationType)
         .HasConversion<string>();

        b.Entity<Debtor>()
         .Property(d => d.Status)
         .HasConversion<string>();
    }
}