using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ISBN), IsUnique = true)]
public class Book
{
    [Key] public long BookId { get; set; }

    [Required, StringLength(32)] public string ISBN { get; set; } = null!;
    [Required] public string Language { get; set; } = null!;
    [Required] public string Location { get; set; } = null!;
    [Required] public string Title { get; set; } = null!;
    [Required] public string Author { get; set; } = null!;
    [Required] public string Genre { get; set; } = null!;

    [Range(1, 9999)] public int PublishYear { get; set; }

    [Required] public string Publisher { get; set; } = null!;
    public string? Description { get; set; }

    [Required, Url] public string CoverUrl { get; set; } = null!;
    [Range(1, int.MaxValue)] public int Pages { get; set; }

    [DefaultValue(1), Range(0, int.MaxValue)]
    public int Amount { get; set; } = 1;

    /* навигации */
    public ICollection<Supply>? Supplies { get; set; }
    public ICollection<Debtor>? Debtors { get; set; }
}