using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    public interface IProductService
    {
        Task<IEnumerable<ProductModel>> GetProductsAsync();
    }
}