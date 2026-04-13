namespace CORNY.Models.ViewModels
{
    public class OrderItemDisplayViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PriceWhenPlaced { get; set; }
    }

    public class OrderDisplayViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalOrderCost { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public IReadOnlyList<OrderItemDisplayViewModel> Items { get; set; } = Array.Empty<OrderItemDisplayViewModel>();
    }
}