using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Interfaces
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext productDbContext;

        public ProductRepository(ProductDbContext productDbContext)
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

        public async Task<bool> UpdateProductAsync(ProductModel product)
        {
            var existingProduct = await productDbContext.Products.FindAsync(product.ProductId);
            if (existingProduct == null)
            {
                return false;
            }
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Pricing = product.Pricing;
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
