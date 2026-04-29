using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuissnessLogicLayer
{
    public class OrderService : IOrderService
    {
        private readonly ProductDbContext productDbContext;

        public OrderService(ProductDbContext productDbContext)
        {
            this.productDbContext = productDbContext;
        }

        public async Task<int?> PlaceOrderAsync(int userId, string? shippingAddress)
        {
            var cartItems = await productDbContext.CartItems
                .Where(item => item.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return null;
            }

            var productIds = cartItems.Select(item => item.ProductId).Distinct().ToList();
            var productLookup = await productDbContext.Products
                .Where(product => productIds.Contains(product.ProductId))
                .ToDictionaryAsync(product => product.ProductId);

            var order = new OrderModel
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Placed",
                ShippingAddress = string.IsNullOrWhiteSpace(shippingAddress) ? "Unknown" : shippingAddress
            };

            decimal total = 0m;
            foreach (var item in cartItems)
            {
                if (!productLookup.TryGetValue(item.ProductId, out var product))
                {
                    continue;
                }

                var lineTotal = product.Pricing * item.Quantity;
                total += lineTotal;

                order.OrderItems.Add(new OrderItemModel
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceWhenPlaced = product.Pricing
                });

                product.InventoryQuantity = Math.Max(0, product.InventoryQuantity - item.Quantity);
                product.IsForSale = product.InventoryQuantity > 0;
            }

            order.TotalOrderCost = total;

            var strategy = productDbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await productDbContext.Database.BeginTransactionAsync();

                productDbContext.Orders.Add(order);
                await productDbContext.SaveChangesAsync();

                productDbContext.CartItems.RemoveRange(cartItems);
                await productDbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                return order.OrderId;
            });
        }

        public async Task<IReadOnlyList<OrderModel>> GetOrdersAsync()
        {
            return await productDbContext.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<OrderModel>> GetOrdersForUserAsync(int userId)
        {
            return await productDbContext.Orders
                .AsNoTracking()
                .Include(order => order.OrderItems)
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();
        }
    }
}