using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Email), IsUnique = true)]
[Table("ReaderTicket")]
public class ReaderTicket
{
    [Key, Column("reader_ticket_id")] 
    public long ReaderTicketId { get; set; }
    [Column("full_name")] 
    public string FullName { get; set; } = null!;
    [Column("email")] 
    public string Email { get; set; } = null!;
    [Column("phone_number")] 
    public string PhoneNumber { get; set; } = null!;
    [Column("extra_phone_number")] 
    public string? ExtraPhoneNumber { get; set; }
    public ICollection<Debtor>? Debtors { get; set; }
}