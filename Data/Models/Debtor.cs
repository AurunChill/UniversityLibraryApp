using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ReaderTicketId), nameof(BookId), IsUnique = true)]
[Table("Debtor")]
public class Debtor
{
    [Key, Column("debtor_id")]
    public long DebtorId { get; set; }
    [Column("reader_ticket_id")]
    public long ReaderTicketId { get; set; }
    [Column("book_id")]
    public long BookId { get; set; }
    [Column("get_date")]
    public DateOnly GetDate { get; set; }
    [Column("return_date")]
    public DateOnly? ReturnDate { get; set; }
    [Column("status")]
    public DebtorStatus Status { get; set; }
    [Column("debt_date")]
    public DateOnly DebtDate { get; set; }
    [Column("days_until_debt")]
    public int DaysUntilDebt { get; set; }
    [Column("late_penalty")]
    public double LatePenalty { get; set; }
    public Book? Book { get; set; }
    public ReaderTicket? ReaderTicket { get; set; }
}