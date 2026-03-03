using DataAccessLayer.Entities;

namespace CORNY.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public ProductModel Product { get; set; } = new();
        public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    }
}