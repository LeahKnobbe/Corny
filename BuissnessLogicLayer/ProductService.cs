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
    }
}