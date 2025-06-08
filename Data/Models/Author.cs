using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Table("Author")]
public class Author
{
    [Key]
    [Column("author_id")]
    public long AuthorId { get; set; }

    [Required, Column("name")]
    public string Name { get; set; } = null!;

    public ICollection<AuthorBook>? Books { get; set; }
}
