using LibraryApp.Data.Models;

namespace LibraryApp.Repositories.Interfaces;

public interface IBookRepository : IRepository<Book, long>
{
    Task<IReadOnlyList<Book>> SearchAsync(BookSearchArgs args);
    Task<IReadOnlyList<Book>> FullTextSearchAsync(string query);
}