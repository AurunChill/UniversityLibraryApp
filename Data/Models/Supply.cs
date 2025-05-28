using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(BookId), nameof(Date))]
[Table("Supply")]
public class Supply
{
    [Key, Column("supply_id")]
    public long SupplyId { get; set; }
    [Column("book_id")]
    public long BookId { get; set; }
    [Column("date")]
    public DateOnly Date { get; set; }
    [Column("operation_type")]
    public OperationType OperationType { get; set; }
    [Column("amount")]
    public int Amount { get; set; }
    public Book? Book { get; set; }
}