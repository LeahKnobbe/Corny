using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    /// <summary>
    /// Service for matching ingredient names to database products.
    /// </summary>
    public interface IProductMatchingService
    {
        /// <summary>
        /// Match suggested ingredient names to actual products in the database.
        /// </summary>
        /// <param name="ingredientNames">List of ingredient names suggested by AI/Spoonacular</param>
        /// <param name="allProducts">All available products from database</param>
        /// <returns>List of matched products</returns>
        Task<IReadOnlyList<ProductModel>> MatchIngredientsToProductsAsync(
            IReadOnlyList<string> ingredientNames,
            IReadOnlyList<ProductModel> allProducts);
    }
}