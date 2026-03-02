using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public interface ICartService
    {
        Task AddToCartAsync(int userId, int productId);
        Task<IReadOnlyList<CartItemModel>> GetCartItemsAsync(int userId);
        Task UpdateQuantityAsync(int userId, int productId, int quantity);
        Task RemoveFromCartAsync(int userId, int productId);
    }
}