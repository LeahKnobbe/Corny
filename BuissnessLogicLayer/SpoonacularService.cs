using BuissnessLogicLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuissnessLogicLayer
{
    public class SpoonacularService : ISpoonacularService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<SpoonacularService> logger;
        private readonly string? apiKey;

        public SpoonacularService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SpoonacularService> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;

            apiKey = configuration["SPOONACULAR_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("SPOONACULAR_API_KEY");

            httpClient.BaseAddress = new Uri("https://api.spoonacular.com/");
        }

        public async Task<SpoonacularRecipe[]?> SearchRecipesByIngredientsAsync(
            IReadOnlyList<string> ingredients,
            int number = 5,
            int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("Spoonacular API key not configured");
                return null;
            }

            if (!ingredients.Any())
            {
                logger.LogWarning("No ingredients provided for recipe search");
                return null;
            }

            try
            {
                var ingredientList = string.Join(",", ingredients);
                var url = $"recipes/findByIngredients?ingredients={Uri.EscapeDataString(ingredientList)}&number={number}&offset={offset}&apiKey={apiKey}&ranking=2&ignorePantry=true";

                logger.LogInformation("Calling Spoonacular API: {Url}", url.Replace(apiKey, "***"));

                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Spoonacular API error: {StatusCode}, Response: {Response}", 
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Spoonacular raw response: {Content}", 
                    content.Substring(0, Math.Min(500, content.Length)));

                var recipes = await response.Content.ReadFromJsonAsync<SpoonacularRecipe[]>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (recipes == null || recipes.Length == 0)
                {
                    logger.LogWarning("Spoonacular returned no recipes");
                    return null;
                }

                logger.LogInformation("Successfully retrieved {Count} recipes from Spoonacular", recipes.Length);
                foreach (var recipe in recipes.Take(3))
                {
                    logger.LogInformation("Recipe: {Title}, ID: {Id}, Image: {Image}", 
                        recipe.Title, recipe.Id, recipe.Image);
                }

                return recipes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling Spoonacular API");
                return null;
            }
        }

        public async Task<SpoonacularRecipeInformation?> GetRecipeInformationAsync(int recipeId)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("Spoonacular API key not configured");
                return null;
            }

            try
            {
                var url = $"recipes/{recipeId}/information?apiKey={apiKey}&includeNutrition=false";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Spoonacular API error getting recipe {RecipeId}: {StatusCode}",
                        recipeId, response.StatusCode);
                    return null;
                }

                var recipe = await response.Content.ReadFromJsonAsync<SpoonacularRecipeInformation>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return recipe;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting recipe information from Spoonacular for recipe {RecipeId}", recipeId);
                return null;
            }
        }
    }
}