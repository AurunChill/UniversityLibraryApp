using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ISBN), IsUnique = true)]
[Table("Books")]
public class Book
{
        [Key]
    [Column("book_id")]
    public long BookId { get; set; }

    [Required, Column("ISBN")]
    public string ISBN { get; set; } = null!;

    [Required, Column("language")]
    public string Language { get; set; } = null!;

    [Required, Column("location")]
    public string Location { get; set; } = null!;

    [Required, Column("title")]
    public string Title { get; set; } = null!;

    [Required, Column("author")]
    public string Author { get; set; } = null!;

    [Required, Column("genre")]
    public string Genre { get; set; } = null!;

    [Column("publish_year")]
    public int PublishYear { get; set; }

    [Required, Column("publisher")]
    public string Publisher { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Required, Column("cover_url")]
    public string CoverUrl { get; set; } = null!;

    [Column("pages")]
    public int Pages { get; set; }

    [Column("amount")]
    public int Amount { get; set; }

    /* навигации */
    public ICollection<Supply>? Supplies { get; set; }
    public ICollection<Debtor>? Debtors { get; set; }
}