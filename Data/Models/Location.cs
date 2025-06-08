using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Data.Models;

[Table("Location")]
public class Location
{
    [Key]
    [Column("location_id")]
    public long LocationId { get; set; }

    [Required, Column("location_name")]
    public string LocationName { get; set; } = null!;

    [Column("amount")]
    public int Amount { get; set; }

    public ICollection<InventoryTransaction>? Transactions { get; set; }
}
