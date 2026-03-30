using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<ProductModel>> GetProductsAsync();
        Task<ProductModel> CreateProductAsync(ProductModel product);
        Task<ProductModel?> GetProductByIdAsync(int id);
        Task<bool> UpdateProductAsync(ProductModel product);
        Task<bool> DeleteProductAsync(int id);
    }
}
