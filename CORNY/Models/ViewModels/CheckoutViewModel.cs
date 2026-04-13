using DataAccessLayer.Entities;

namespace CORNY.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string UserFullName { get; set; } = string.Empty;
        public IReadOnlyList<CartItemViewModel> Items { get; set; } = Array.Empty<CartItemViewModel>();
        public decimal Subtotal => Items.Sum(i => i.LineTotal);
        public decimal Delivery => 7.99m;
        public decimal Taxes => Math.Round(Subtotal * 0.1025m, 2);
        public decimal Total => Subtotal + Delivery + Taxes;

        /// <summary>Distinct product count (3 watermelons + 1 apple = 2 items).</summary>
        public int ItemCount => Items.Count;
    }
}