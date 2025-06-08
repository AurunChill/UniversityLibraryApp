using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Data.Models;

[Table("ReaderTicket")]
public class ReaderTicket
{
    [Key]
    [Column("reader_id")]
    public long ReaderId { get; set; }

    [Required, Column("registration_date")]
    public DateOnly RegistrationDate { get; set; }

    [Column("end_time")]
    public DateOnly? EndTime { get; set; }

    public Reader? Reader { get; set; }
    public ICollection<Debt>? Debts { get; set; }
}
