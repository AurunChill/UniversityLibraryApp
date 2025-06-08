using Microsoft.Data.Sqlite;

namespace LibraryApp.Data
{
    public static class DatabaseInitializer
    {
        public static void EnsureCreated(string dbName = "library.db")
        {
            string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", dbName);
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

        private static readonly string[] CreateTableScripts =
        {
            @"CREATE TABLE IF NOT EXISTS Publisher (
  publisher_id   INTEGER PRIMARY KEY AUTOINCREMENT,
  name           TEXT    NOT NULL UNIQUE
);",
            @"CREATE TABLE IF NOT EXISTS LanguageCode (
  lang_id        INTEGER PRIMARY KEY AUTOINCREMENT,
  code           TEXT    NOT NULL UNIQUE
);",
            @"CREATE TABLE IF NOT EXISTS Genre (
  genre_id       INTEGER PRIMARY KEY AUTOINCREMENT,
  name           TEXT    NOT NULL UNIQUE
);",
            @"CREATE TABLE IF NOT EXISTS Author (
  author_id      INTEGER PRIMARY KEY AUTOINCREMENT,
  name           TEXT    NOT NULL UNIQUE
);",
            @"CREATE TABLE IF NOT EXISTS Book (
  book_id        INTEGER PRIMARY KEY AUTOINCREMENT,
  ISBN           TEXT    NOT NULL UNIQUE,
  publisher_id   INTEGER REFERENCES Publisher(publisher_id) ON DELETE SET NULL,
  publish_year   INTEGER,
  lang_id        INTEGER NOT NULL REFERENCES LanguageCode(lang_id) ON DELETE RESTRICT,
  title          TEXT    NOT NULL,
  description    TEXT,
  pages          INTEGER,
  cover_url      TEXT
);",
            @"CREATE TABLE IF NOT EXISTS Location (
  location_id    INTEGER PRIMARY KEY AUTOINCREMENT,
  location_name  TEXT    NOT NULL UNIQUE,
  amount         INTEGER NOT NULL DEFAULT 0
);",
            @"CREATE TABLE IF NOT EXISTS InventoryTransactions (
  inv_trans_id     INTEGER PRIMARY KEY AUTOINCREMENT,
  book_id          INTEGER NOT NULL REFERENCES Book(book_id) ON DELETE CASCADE,
  location_id      INTEGER NOT NULL REFERENCES Location(location_id) ON DELETE CASCADE,
  prev_location_id INTEGER        REFERENCES Location(location_id) ON DELETE CASCADE,
  date             TEXT    NOT NULL,
  amount           INTEGER NOT NULL
);",
            @"CREATE TABLE IF NOT EXISTS GenreBook (
  genre_book_id   INTEGER PRIMARY KEY AUTOINCREMENT,
  genre_id        INTEGER REFERENCES Genre(genre_id) ON DELETE SET NULL,
  book_id         INTEGER NOT NULL REFERENCES Book(book_id) ON DELETE CASCADE,
  UNIQUE(genre_id, book_id)
);",
            @"CREATE TABLE IF NOT EXISTS AuthorBook (
  author_book_id  INTEGER PRIMARY KEY AUTOINCREMENT,
  author_id       INTEGER REFERENCES Author(author_id) ON DELETE SET NULL,
  book_id         INTEGER NOT NULL REFERENCES Book(book_id) ON DELETE CASCADE,
  UNIQUE(author_id, book_id)
);",
            @"CREATE TABLE IF NOT EXISTS BookLanguage (
  book_language_id INTEGER PRIMARY KEY AUTOINCREMENT,
  book_id          INTEGER NOT NULL REFERENCES Book(book_id) ON DELETE CASCADE,
  lang_id          INTEGER NOT NULL REFERENCES LanguageCode(lang_id) ON DELETE CASCADE,
  UNIQUE(book_id, lang_id)
);",
            @"CREATE TABLE IF NOT EXISTS Reader (
  reader_id   INTEGER PRIMARY KEY AUTOINCREMENT,
  full_name   TEXT    NOT NULL,
  email       TEXT    NOT NULL UNIQUE,
  phone       TEXT    UNIQUE
);",
            @"CREATE TABLE IF NOT EXISTS ReaderTicket (
  reader_id         INTEGER PRIMARY KEY REFERENCES Reader(reader_id) ON DELETE CASCADE,
  registration_date TEXT    NOT NULL,
  end_time          TEXT
);",
            @"CREATE TABLE IF NOT EXISTS Debts (
  debt_id            INTEGER PRIMARY KEY AUTOINCREMENT,
  book_id            INTEGER NOT NULL REFERENCES Book(book_id) ON DELETE CASCADE,
  reader_ticket_id   INTEGER NOT NULL REFERENCES ReaderTicket(reader_id) ON DELETE CASCADE,
  start_time         TEXT    NOT NULL,
  end_time           TEXT
);"
        };
    }
}
