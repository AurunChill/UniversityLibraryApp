using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LibraryApp.Data;
using LibraryApp.Data.Models;
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

        /* 👉 вместо AddSqlite используем AddDbContextFactory
            (контекст остаётся тем же, просто получаем фабрику) */
        builder.Services.AddDbContextFactory<LibraryContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddScoped<MainForm>();
        return builder.Build();
    }

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using IHost host = CreateHost(args);

        // даём Books доступ к той же фабрике, что зарегистрирована в DI
        var factory = host.Services.GetRequiredService<IDbContextFactory<LibraryContext>>();
        Books.Init(factory);
        Supplies.Factory  = factory;
        ReaderTickets.Init(factory);
        Debtors.Init(factory);

        DatabaseInitializer.EnsureCreated(
            args.Length > 0 && args[0] == "--test" ? "library_test.db" : "library.db");

        Application.Run(host.Services.GetRequiredService<MainForm>());
    }
}