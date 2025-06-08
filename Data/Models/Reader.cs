using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Email), IsUnique = true)]
[Table("Reader")]
public class Reader
{
    [Key]
    [Column("reader_id")]
    public long ReaderId { get; set; }

    [Required, Column("full_name")]
    public string FullName { get; set; } = null!;

    [Required, Column("email")]
    public string Email { get; set; } = null!;

    [Column("phone")]
    public string? Phone { get; set; }

    public ReaderTicket? Ticket { get; set; }
}
