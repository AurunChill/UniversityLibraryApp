using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LibraryApp.Data;
using LibraryApp.Repositories;
using LibraryApp.Repositories.Interfaces;
using LibraryApp.Services;
using LibraryApp.Services.Interfaces;
using LibraryApp.UI.Forms;

namespace LibraryApp;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // ─── 1. Выбираем имя файла БД ─────────────────────────
        string dbName = args.Length > 0 && args[0] == "--test"
            ? "library_test.db"
            : "library.db";

        using IHost host = CreateHost(args);
        DatabaseInitializer.EnsureCreated(dbName);

        Application.Run(host.Services.GetRequiredService<MainForm>());
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        string dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "AppData",
            args.Length > 0 && args[0] == "--test" ? "library_test.db" : "library.db"
        );

        // регистрируем SQLite-контекст
        builder.Services.AddSqlite<LibraryContext>($"Data Source={dbPath}");

        // регистрируем репозитории и сервисы
        builder.Services.AddScoped<IBookRepository, BookRepository>();
        builder.Services.AddScoped<IBookService,    BookService>();
        builder.Services.AddScoped<MainForm>();

        return builder.Build();
    }
}