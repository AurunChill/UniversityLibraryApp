using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LibraryApp.Data;
using LibraryApp.Repositories;
using LibraryApp.Repositories.Interfaces;
using LibraryApp.Services;
using LibraryApp.Services.Interfaces;

namespace LibraryApp;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using IHost host = CreateHost(args);
        // применяем миграции / создаём БД
        host.Services.GetRequiredService<LibraryContext>()
                     .Database.Migrate();

        // запускаем главное окно
        // Application.Run(host.Services.GetRequiredService<MainForm>());
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // путь к файлу БД (--test => отдельная БД)
        string dbPath = args.Length > 0 && args[0] == "--test"
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "library_test.db")
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "library.db");

        // регистрируем DbContext
        builder.Services.AddSqlite<LibraryContext>($"Data Source={dbPath}");

        // репо-/сервис-слой
        builder.Services.AddScoped<IBookRepository, BookRepository>();
        builder.Services.AddScoped<IBookService,    BookService>();

        // регистрация форм
        // builder.Services.AddScoped<MainForm>();

        return builder.Build();
    }
}
