using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Data.Models;

[Table("Debts")]
public class Debt
{
    [Key]
    [Column("debt_id")]
    public long DebtId { get; set; }

    [Required, Column("book_id")]
    public long BookId { get; set; }
    public Book? Book { get; set; }

    [Required, Column("reader_ticket_id")]
    public long ReaderTicketId { get; set; }
    public ReaderTicket? ReaderTicket { get; set; }

    [Required, Column("start_time")]
    public DateOnly StartTime { get; set; }

    [Column("end_time")]
    public DateOnly? EndTime { get; set; }
}
