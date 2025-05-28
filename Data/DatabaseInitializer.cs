using Microsoft.Data.Sqlite;

namespace LibraryApp.Data
{
    /// <summary>Создаёт файл SQLite и все таблицы, если их нет.</summary>
    public static class DatabaseInitializer
    {
        /// <summary>Запуск при старте приложения.</summary>
        public static void EnsureCreated(string dbName = "library.db")
        {
            string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", dbName);
            Console.WriteLine(DbPath);
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

            var connectionString = $"Data Source={DbPath};Cache=Shared";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();
            foreach (var cmdText in CreateTableScripts)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = cmdText;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        // --- SQL DDL -------------------------------

        private static readonly string[] CreateTableScripts =
        {
            // Books
            @"CREATE TABLE IF NOT EXISTS Books (
                book_id      INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
                ISBN         TEXT    NOT NULL UNIQUE,
                language     TEXT    NOT NULL,
                location     TEXT    NOT NULL,
                title        TEXT    NOT NULL,
                author       TEXT    NOT NULL,
                genre        TEXT    NOT NULL,
                publish_year INTEGER NOT NULL CHECK(publish_year > 0),
                publisher    TEXT    NOT NULL,
                description  TEXT,
                cover_url    TEXT    NOT NULL,
                pages        INTEGER NOT NULL CHECK(pages > 0),
                amount       INTEGER NOT NULL DEFAULT 1 CHECK(amount >= 0)
            );",

            // Supply
            @"CREATE TABLE IF NOT EXISTS Supply (
                supply_id      INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
                book_id        INTEGER NOT NULL REFERENCES Books(book_id) ON DELETE CASCADE,
                date           TEXT    NOT NULL,              -- ISO yyyy-MM-dd
                operation_type TEXT    NOT NULL CHECK(operation_type IN ('приход','списание','долг')),
                amount         INTEGER NOT NULL
            );",

            // ReaderTicket
            @"CREATE TABLE IF NOT EXISTS ReaderTicket (
                reader_ticket_id  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
                full_name         TEXT    NOT NULL,
                email             TEXT    NOT NULL UNIQUE,
                phone_number      TEXT    NOT NULL,
                extra_phone_number TEXT
            );",

            // Debtor
            @"CREATE TABLE IF NOT EXISTS Debtor (
                debtor_id        INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,
                reader_ticket_id INTEGER NOT NULL REFERENCES ReaderTicket(reader_ticket_id) ON DELETE CASCADE,
                book_id          INTEGER NOT NULL REFERENCES Books(book_id) ON DELETE CASCADE,
                get_date         TEXT    NOT NULL,
                return_date      TEXT,
                status           TEXT    NOT NULL DEFAULT 'в срок'
                                 CHECK(status IN ('в срок',
                                                   'просрочено возвращено без оплаты',
                                                   'утеряно',
                                                   'просрочено возвращено с оплатой')),
                debt_date        TEXT    NOT NULL,
                days_until_debt  INTEGER NOT NULL CHECK(days_until_debt >= 0),
                late_penalty     REAL    NOT NULL DEFAULT 0.0 CHECK(late_penalty >= 0)
            );"
        };
    }
}
