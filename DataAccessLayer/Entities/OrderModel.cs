using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("Order")]
    public class OrderModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Order_ID")]
        public int OrderId { get; set; }

        [Column("Order_Date")]
        public DateTime OrderDate { get; set; }

        [Column("Total_Order_Cost", TypeName = "decimal(18,2)")]
        public decimal TotalOrderCost { get; set; }

        [Column("Status")]
        public string Status { get; set; } = string.Empty;

        [Column("Shipping_Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Column("UserId")]
        public int UserId { get; set; }

        public ICollection<OrderItemModel> OrderItems { get; set; } = new List<OrderItemModel>();
    }
}