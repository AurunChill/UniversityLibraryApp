using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Models;

[Index(nameof(Code), IsUnique = true)]
[Table("LanguageCode")]
public class LanguageCode
{
    [Key]
    [Column("lang_id")]
    public long LangId { get; set; }

    [Required, Column("code")]
    public string Code { get; set; } = null!;

    public ICollection<Book>? Books { get; set; }
}
