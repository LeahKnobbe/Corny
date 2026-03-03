using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("ProductImage")]
    public class ProductImageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductImageId { get; set; }

        public int ProductId { get; set; }

        public int SortOrder { get; set; }

        [Column("Image_Url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}