using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Name), IsUnique = true)]
[Table("Genre")]
public class Genre
{
    [Key]
    [Column("genre_id")]
    public long GenreId { get; set; }

    [Required, Column("name")]
    public string Name { get; set; } = null!;

    public ICollection<GenreBook>? Books { get; set; }
}
