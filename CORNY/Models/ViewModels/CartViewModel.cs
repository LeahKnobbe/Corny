using DataAccessLayer.Entities;

namespace CORNY.Models.ViewModels
{
    public class CartItemViewModel
    {
        public ProductModel Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal LineTotal => Product.Pricing * Quantity;
    }

    public class CartViewModel
    {
        public IReadOnlyList<CartItemViewModel> Items { get; set; } = Array.Empty<CartItemViewModel>();
        public decimal Total => Items.Sum(item => item.LineTotal);
    }
}