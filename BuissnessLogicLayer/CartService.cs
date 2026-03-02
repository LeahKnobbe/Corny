using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuissnessLogicLayer
{
    public class CartService : ICartService
    {
        private readonly ProductDbContext productDbContext;

        public CartService(ProductDbContext productDbContext)
        {
            this.productDbContext = productDbContext;
        }

        public async Task AddToCartAsync(int userId, int productId)
        {
            var existingItem = await productDbContext.CartItems
                .FirstOrDefaultAsync(item => item.UserId == userId && item.ProductId == productId);

            if (existingItem == null)
            {
                productDbContext.CartItems.Add(new CartItemModel
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = 1
                });
            }
            else
            {
                existingItem.Quantity += 1;
            }

            await productDbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<CartItemModel>> GetCartItemsAsync(int userId)
        {
            return await productDbContext.CartItems
                .Where(item => item.UserId == userId)
                .ToListAsync();
        }

        public async Task UpdateQuantityAsync(int userId, int productId, int quantity)
        {
            var item = await productDbContext.CartItems
                .FirstOrDefaultAsync(i => i.UserId == userId && i.ProductId == productId);

            if (item == null)
            {
                return;
            }

            if (quantity <= 0)
            {
                productDbContext.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            await productDbContext.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int userId, int productId)
        {
            var item = await productDbContext.CartItems
                .FirstOrDefaultAsync(i => i.UserId == userId && i.ProductId == productId);

            if (item != null)
            {
                productDbContext.CartItems.Remove(item);
                await productDbContext.SaveChangesAsync();
            }
        }
    }
}