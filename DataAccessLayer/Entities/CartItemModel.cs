using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("CartItem")]
    public class CartItemModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartItemId { get; set; }

        [Column("Cart_ID")]
        public int CartId { get; set; }

        public CartModel? Cart { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("ProductId")]
        public int ProductId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }
    }
}