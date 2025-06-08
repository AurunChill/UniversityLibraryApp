using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Data.Models;

[Table("InventoryTransactions")]
public class InventoryTransaction
{
    [Key]
    [Column("inv_trans_id")]
    public long InventoryTransactionId { get; set; }

    [Required, Column("book_id")]
    public long BookId { get; set; }
    public Book? Book { get; set; }

    [Required, Column("location_id")]
    public long LocationId { get; set; }
    public Location? Location { get; set; }

    [Column("prev_location_id")]
    public long? PrevLocationId { get; set; }
    public Location? PrevLocation { get; set; }

    [Required, Column("date")]
    public DateOnly Date { get; set; }

    [Column("amount")]
    public int Amount { get; set; }
}
