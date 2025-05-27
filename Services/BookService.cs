using LibraryApp.Data.Models;
using LibraryApp.Repositories.Interfaces;
using LibraryApp.Services.Interfaces;

namespace LibraryApp.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _repo;
    public BookService(IBookRepository repo) => _repo = repo;

    // -------- CRUD --------
    public Task<IReadOnlyList<Book>> GetAllAsync(int? limit, int offset) =>
        _repo.GetAllAsync(limit, offset);

    public Task<Book?> GetAsync(long id) => _repo.GetByIdAsync(id);

    public async Task<Book> CreateAsync(Book dto)
    {
        var dup = await _repo.SearchAsync(new BookSearchArgs(ISBN: dto.ISBN));
        if (dup.Any()) throw new InvalidOperationException("ISBN уже существует.");
        return await _repo.AddAsync(dto);
    }

    public async Task<bool> UpdateAsync(Book dto)
    {
        var original = await _repo.GetByIdAsync(dto.BookId)
                      ?? throw new KeyNotFoundException("Не найдено.");
        if (original.ISBN != dto.ISBN)
            throw new InvalidOperationException("ISBN нельзя менять.");
        return await _repo.UpdateAsync(dto);
    }

    public Task<bool> DeleteAsync(long id) => _repo.DeleteAsync(id);

    // -------- Поиск --------
    public Task<IReadOnlyList<Book>> SearchAsync(BookSearchArgs a) => _repo.SearchAsync(a);
    public Task<IReadOnlyList<Book>> FullTextAsync(string q) => _repo.FullTextSearchAsync(q);
}