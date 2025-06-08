using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ISBN), IsUnique = true)]
[Table("Book")]
public class Book
{
    [Key]
    [Column("book_id")]
    public long BookId { get; set; }

    [Required, Column("ISBN")]
    public string ISBN { get; set; } = null!;

    [Column("publisher_id")]
    public long? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }

    [Column("publish_year")]
    public int? PublishYear { get; set; }

    [Required, Column("lang_id")]
    public long LangId { get; set; }
    public LanguageCode? Language { get; set; }

    [Required, Column("title")]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("pages")]
    public int? Pages { get; set; }

    [Column("cover_url")]
    public string? CoverUrl { get; set; }

    public ICollection<GenreBook>? Genres { get; set; }
    public ICollection<AuthorBook>? Authors { get; set; }
    public ICollection<InventoryTransaction>? InventoryTransactions { get; set; }
    public ICollection<Debt>? Debts { get; set; }
}
