using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(ReaderTicketId), nameof(BookId), IsUnique = true)]
public class Debtor
{
    [Key] public long DebtorId { get; set; }

    [Required] public long ReaderTicketId { get; set; }
    [Required] public long BookId { get; set; }

    [Required] public DateOnly GetDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    [Required] public DebtorStatus Status { get; set; } = DebtorStatus.В_Срок;

    [Required] public DateOnly DebtDate { get; set; }

    [Range(0, int.MaxValue)]
    public int DaysUntilDebt { get; set; }

    [DefaultValue(0.0), Range(0, double.MaxValue)]
    public double LatePenalty { get; set; }

    public ReaderTicket? ReaderTicket { get; set; }
    public Book? Book { get; set; }
}