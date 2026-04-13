using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductModel> Products { get; set; }

        public DbSet<CartModel> Carts { get; set; }

        public DbSet<CartItemModel> CartItems { get; set; }

        public DbSet<ProductImageModel> ProductImages { get; set; }

        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderItemModel> OrderItems { get; set; }
    }
}