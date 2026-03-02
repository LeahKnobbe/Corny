using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public interface IProductService
    {
        Task<IEnumerable<ProductModel>> GetProductsAsync();

        Task<ProductModel> CreateProductAsync(ProductModel product);
        Task<ProductModel?> GetProductByIdAsync(int id);
        Task<IReadOnlyList<ProductModel>> GetProductsByIdsAsync(IEnumerable<int> productIds);
        Task<bool> UpdateProductAsync(ProductModel product);
        Task<bool> DeleteProductAsync(int id);
    }
}