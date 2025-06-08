using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(BookId), nameof(LangId), IsUnique = true)]
[Table("BookLanguage")]
public class BookLanguage
{
    [Key]
    [Column("book_language_id")]
    public long BookLanguageId { get; set; }

    [Required, Column("book_id")]
    public long BookId { get; set; }
    public Book? Book { get; set; }

    [Required, Column("lang_id")]
    public long LangId { get; set; }
    public LanguageCode? Language { get; set; }
}
