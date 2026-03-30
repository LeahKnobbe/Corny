namespace CORNY.Models.ViewModels
{
    public class ProductImageItemViewModel
    {
        public int? ProductImageId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }
    }
}