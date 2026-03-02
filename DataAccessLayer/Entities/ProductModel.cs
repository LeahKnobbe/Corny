
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("Product")]
    public class ProductModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Product_ID")]
        public int ProductId { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("Description")]
        public string? Description { get; set; }

        [Column("Sizing")]
        public string? Sizing { get; set; }

        [Column("Inventory_Quantity")]
        public int InventoryQuantity { get; set; }

        [Column("Is_For_Sale")]
        public bool IsForSale { get; set; }

        [Column("Pricing", TypeName = "decimal(18,2)")]
        public decimal Pricing { get; set; }

        [Column("Farm_ID")]
        public int FarmId { get; set; }

        [Column("Category_ID")]
        public int CategoryId { get; set; }
    }
}