using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(AuthorId), nameof(BookId), IsUnique = true)]
[Table("AuthorBook")]
public class AuthorBook
{
    [Key]
    [Column("author_book_id")]
    public long AuthorBookId { get; set; }

    [Column("author_id")]
    public long? AuthorId { get; set; }
    public Author? Author { get; set; }

    [Required, Column("book_id")]
    public long BookId { get; set; }
    public Book? Book { get; set; }
}
