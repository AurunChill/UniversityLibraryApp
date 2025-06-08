using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Table("Publisher")]
public class Publisher
{
    [Key]
    [Column("publisher_id")]
    public long PublisherId { get; set; }

    [Required, Column("name")]
    public string Name { get; set; } = null!;

    public ICollection<Book>? Books { get; set; }
}
