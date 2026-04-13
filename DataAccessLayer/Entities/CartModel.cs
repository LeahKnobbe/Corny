using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("Cart")]
    public class CartModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Cart_ID")]
        public int CartId { get; set; }

        [Column("Status")]
        public string Status { get; set; } = string.Empty;

        [Column("Create_Date")]
        public DateTime CreateDate { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        public ICollection<CartItemModel> CartItems { get; set; } = new List<CartItemModel>();
    }
}