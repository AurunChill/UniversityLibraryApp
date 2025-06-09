using LibraryApp.Data;
using LibraryApp.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryApp.Tests;

public class DatabaseTests
{
    [Fact]
    public void CanAddAndRetrieveBook()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlite(connection)
            .Options;
        using var context = new LibraryContext(options);
        context.Database.EnsureCreated();

        var lang = new LanguageCode { Code = "ru" };
        context.LanguageCodes.Add(lang);
        context.SaveChanges();

        var book = new Book { ISBN = "1", Title = "Test", LangId = lang.LangId };
        context.Books.Add(book);
        context.SaveChanges();

        var loaded = context.Books.Include(b => b.Language).First();
        Assert.Equal("Test", loaded.Title);
        Assert.Equal("ru", loaded.Language!.Code);
    }
}
