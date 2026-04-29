using BuissnessLogicLayer.Models;
using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    /// <summary>
    /// Service for generating AI-powered recipe suggestions based on cart items.
    /// </summary>
    public interface IRecipeSuggestionService
    {
        /// <summary>
        /// Get recipe suggestions based on the user's cart items and available products.
        /// </summary>
        /// <param name="cartItems">Items currently in the user's cart</param>
        /// <param name="availableProducts">All products available in the database</param>
        /// <param name="filter">Optional filter (e.g., "vegetarian", "high-protein", "quick-meals")</param>
        /// <param name="offset">Optional offset for pagination</param>
        /// <returns>Recipe suggestions and recommended add-on products</returns>
        Task<RecipeSuggestionResult> GetRecipeSuggestionsAsync(
            IReadOnlyList<CartItemInfo> cartItems,
            IReadOnlyList<ProductModel> availableProducts,
            string? filter = null,
            int offset = 0);
    }

    public class RecipeSuggestionResult
    {
        public IReadOnlyList<RecipeCardViewModel> Recipes { get; set; } = Array.Empty<RecipeCardViewModel>();
        public IReadOnlyList<AddonProductViewModel> SuggestedAddons { get; set; } = Array.Empty<AddonProductViewModel>();
    }
}