using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(GenreId), nameof(BookId), IsUnique = true)]
[Table("GenreBook")]
public class GenreBook
{
    [Key]
    [Column("genre_book_id")]
    public long GenreBookId { get; set; }

    [Column("genre_id")]
    public long? GenreId { get; set; }
    public Genre? Genre { get; set; }

    [Required, Column("book_id")]
    public long BookId { get; set; }
    public Book? Book { get; set; }
}
