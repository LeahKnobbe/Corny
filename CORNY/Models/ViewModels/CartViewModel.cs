using DataAccessLayer.Entities;

namespace CORNY.Models.ViewModels
{
    public class CartItemViewModel
    {
        public ProductModel Product { get; set; } = new();
        public int Quantity { get; set; }

        // Cart thumbnail (optional). If null/empty or fails to load, the view will fall back to a placeholder.
        public string? ImageUrl { get; set; }

        public decimal LineTotal => Product.Pricing * Quantity;
    }

    public class CartViewModel
    {
        public IReadOnlyList<CartItemViewModel> Items { get; set; } = Array.Empty<CartItemViewModel>();
        public decimal Total => Items.Sum(item => item.LineTotal);
    }
}