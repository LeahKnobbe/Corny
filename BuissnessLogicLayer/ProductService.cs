using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuissnessLogicLayer
{
    public class ProductService : IProductService
    {
        private readonly ProductDbContext productDbContext;

        public ProductService(ProductDbContext productDbContext)
        {
            this.productDbContext = productDbContext;
        }

        public async Task<IEnumerable<ProductModel>> GetProductsAsync()
        {
            return await productDbContext.Products.ToListAsync();
        }

        public async Task<ProductModel> CreateProductAsync(ProductModel product)
        {
            productDbContext.Products.Add(product);
            await productDbContext.SaveChangesAsync();
            return product;
        }   

        public async Task<ProductModel?> GetProductByIdAsync(int id)
        {
            return await productDbContext.Products.FindAsync(id);
        }

        public async Task<IReadOnlyList<ProductModel>> GetProductsByIdsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return Array.Empty<ProductModel>();
            }

            return await productDbContext.Products
                .Where(product => ids.Contains(product.ProductId))
                .ToListAsync();
        }

        public async Task<bool> UpdateProductAsync(ProductModel product)
        {
            var existingProduct = await productDbContext.Products.FindAsync(product.ProductId);
            if (existingProduct == null)
            {
                return false;
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Sizing = product.Sizing;
            existingProduct.InventoryQuantity = product.InventoryQuantity;
            existingProduct.IsForSale = product.IsForSale;
            existingProduct.Pricing = product.Pricing;
            existingProduct.FarmId = product.FarmId;
            existingProduct.CategoryId = product.CategoryId;

            await productDbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await productDbContext.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }
            productDbContext.Products.Remove(product);
            await productDbContext.SaveChangesAsync();
            return true;
        }
    }
}