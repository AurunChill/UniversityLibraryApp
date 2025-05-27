using LibraryApp.Data.Models;

namespace LibraryApp.Services.Interfaces;

public interface IBookService
{
    Task<IReadOnlyList<Book>> GetAllAsync(int? limit, int offset);
    Task<Book?> GetAsync(long id);
    Task<Book> CreateAsync(Book dto);
    Task<bool> UpdateAsync(Book dto);
    Task<bool> DeleteAsync(long id);

    Task<IReadOnlyList<Book>> SearchAsync(BookSearchArgs args);
    Task<IReadOnlyList<Book>> FullTextAsync(string q);
}