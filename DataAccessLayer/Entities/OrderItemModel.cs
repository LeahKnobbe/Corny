using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("Order_Item")]
    public class OrderItemModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Order_Item_ID")]
        public int OrderItemId { get; set; }

        [Column("Order_ID")]
        public int OrderId { get; set; }

        public OrderModel? Order { get; set; }

        [Column("Product_ID")]
        public int ProductId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("Price_When_Placed", TypeName = "decimal(18,2)")]
        public decimal PriceWhenPlaced { get; set; }
    }
}