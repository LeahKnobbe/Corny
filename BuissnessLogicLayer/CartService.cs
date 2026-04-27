using System;
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

        public async Task AddToCartAsync(int userId, int productId, int quantity)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            var product = await productDbContext.Products
                .FirstOrDefaultAsync(item => item.ProductId == productId);

            if (product == null || product.InventoryQuantity <= 0)
            {
                return;
            }

            var cart = await GetOrCreateCartAsync(userId);

            var existingItem = await productDbContext.CartItems
                .FirstOrDefaultAsync(item => item.CartId == cart.CartId && item.ProductId == productId);

            var available = product.InventoryQuantity;
            var existingQuantity = existingItem?.Quantity ?? 0;
            var newQuantity = Math.Min(available, existingQuantity + quantity);

            if (newQuantity <= 0)
            {
                return;
            }

            if (existingItem == null)
            {
                productDbContext.CartItems.Add(new CartItemModel
                {
                    CartId = cart.CartId,
                    UserId = userId,
                    ProductId = productId,
                    Quantity = newQuantity
                });
            }
            else
            {
                existingItem.Quantity = newQuantity;
            }

            await productDbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<CartItemModel>> GetCartItemsAsync(int userId)
        {
            var cart = await productDbContext.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return Array.Empty<CartItemModel>();
            }

            return await productDbContext.CartItems
                .Where(item => item.CartId == cart.CartId)
                .ToListAsync();
        }

        public async Task UpdateQuantityAsync(int userId, int productId, int quantity)
        {
            var cart = await productDbContext.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return;
            }

            var item = await productDbContext.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.CartId && i.ProductId == productId);

            if (item == null)
            {
                return;
            }

            var product = await productDbContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            var available = product?.InventoryQuantity ?? 0;

            if (quantity <= 0 || available <= 0)
            {
                productDbContext.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = Math.Min(quantity, available);
            }

            await productDbContext.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int userId, int productId)
        {
            var cart = await productDbContext.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return;
            }

            var item = await productDbContext.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.CartId && i.ProductId == productId);

            if (item != null)
            {
                productDbContext.CartItems.Remove(item);
                await productDbContext.SaveChangesAsync();
            }
        }

        private async Task<CartModel> GetOrCreateCartAsync(int userId)
        {
            var cart = await productDbContext.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                return cart;
            }

            cart = new CartModel
            {
                UserId = userId,
                Status = "Open",
                CreateDate = DateTime.UtcNow
            };

            productDbContext.Carts.Add(cart);
            await productDbContext.SaveChangesAsync();
            return cart;
        }
    }
}