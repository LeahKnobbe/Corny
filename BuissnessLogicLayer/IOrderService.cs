using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public interface IOrderService
    {
        Task<int?> PlaceOrderAsync(int userId, string? shippingAddress);
        Task<IReadOnlyList<OrderModel>> GetOrdersAsync();
        Task<IReadOnlyList<OrderModel>> GetOrdersForUserAsync(int userId);
    }
}