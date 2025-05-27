using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(BookId), nameof(Date))]
public class Supply
{
    [Key] public long SupplyId { get; set; }

    [Required] public long BookId { get; set; }
    [Required] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required] public OperationType OperationType { get; set; }

    [Range(int.MinValue, int.MaxValue)]
    public int Amount { get; set; }

    /* навигация */
    public Book? Book { get; set; }
}