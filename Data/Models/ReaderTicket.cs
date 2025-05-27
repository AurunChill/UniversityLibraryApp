using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Email), IsUnique = true)]
public class ReaderTicket
{
    [Key] public long ReaderTicketId { get; set; }

    [Required] public string FullName { get; set; } = null!;
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required, Phone] public string PhoneNumber { get; set; } = null!;
    public string? ExtraPhoneNumber { get; set; }

    public ICollection<Debtor>? Debtors { get; set; }
}