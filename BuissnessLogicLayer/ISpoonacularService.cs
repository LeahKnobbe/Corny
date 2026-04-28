using BuissnessLogicLayer.Models;

namespace BuissnessLogicLayer
{
    /// <summary>
    /// Service for interacting with the Spoonacular API to find recipes based on ingredients.
    /// </summary>
    public interface ISpoonacularService
    {
        /// <summary>
        /// Search for recipes based on the given ingredients.
        /// </summary>
        /// <param name="ingredients">List of ingredient names from the cart</param>
        /// <param name="number">Number of results to return (default: 5)</param>
        /// <returns>Array of recipe search results from Spoonacular</returns>
        Task<SpoonacularRecipe[]?> SearchRecipesByIngredientsAsync(
            IReadOnlyList<string> ingredients, 
            int number = 5);

        /// <summary>
        /// Get detailed information about a specific recipe by ID, including ingredients and instructions.
        /// </summary>
        /// <param name="recipeId">Spoonacular recipe ID</param>
        /// <returns>Detailed recipe information with instructions</returns>
        Task<SpoonacularRecipeInformation?> GetRecipeInformationAsync(int recipeId);
    }
}