using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LibraryApp.Data;
using LibraryApp.Data.Services;
using LibraryApp.UI.Forms;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp;

internal static class Program
{
    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        string dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "AppData",
            args.Length > 0 && args[0] == "--test" ? "library_test.db" : "library.db"
        );

        builder.Services.AddDbContextFactory<LibraryContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddScoped<BookService>();
        builder.Services.AddScoped<InventoryTransactionService>();
        builder.Services.AddScoped<DebtService>();
        builder.Services.AddScoped<ReaderService>();
        builder.Services.AddScoped<ReaderTicketService>();
        builder.Services.AddScoped<AuthorService>();
        builder.Services.AddScoped<GenreService>();
        builder.Services.AddScoped<PublisherService>();
        builder.Services.AddScoped<LanguageCodeService>();
        builder.Services.AddScoped<LocationService>();

        builder.Services.AddScoped<InventoryPage>();

        builder.Services.AddScoped<MainForm>();
        builder.Services.AddScoped<DebtsPage>();
        builder.Services.AddScoped<ReadersPage>();
        builder.Services.AddScoped<BookDetailForm>();
        return builder.Build();
    }

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using IHost host = CreateHost(args);

        DatabaseInitializer.EnsureCreated(
            args.Length > 0 && args[0] == "--test" ? "library_test.db" : "library.db");

        Application.Run(host.Services.GetRequiredService<MainForm>());
    }
}
