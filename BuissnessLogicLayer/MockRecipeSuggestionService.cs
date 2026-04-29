using BuissnessLogicLayer.Models;
using DataAccessLayer.Entities;

namespace BuissnessLogicLayer
{
    /// <summary>
    /// Mock implementation of recipe suggestion service.
    /// Returns hardcoded sample data until OpenAI integration is added.
    /// </summary>
    public class MockRecipeSuggestionService : IRecipeSuggestionService
    {
        public Task<RecipeSuggestionResult> GetRecipeSuggestionsAsync(
            IReadOnlyList<CartItemInfo> cartItems,
            IReadOnlyList<ProductModel> availableProducts,
            string? filter = null,
            int offset = 0)
        {
            // Mock recipes based on the design
            var recipes = new List<RecipeCardViewModel>
            {
                new RecipeCardViewModel
                {
                    Title = "Honey Glazed Chicken",
                    Description = "Juicy chicken glazed with honey and garlic, served with roasted veggies.",
                    ImageUrl = "/images/recipes/honey-glazed-chicken.jpg",
                    UsesCartItems = 3,
                    Difficulty = "Easy",
                    PrepTime = "25 min",
                    Tags = new[] { "Uses 3 cart items", "Easy", "25 min" }
                },
                new RecipeCardViewModel
                {
                    Title = "Apple Honey Salad",
                    Description = "Crisp apples, mixed greens, feta, and walnuts tossed in a honey lemon vinaigrette.",
                    ImageUrl = "/images/recipes/apple-honey-salad.jpg",
                    UsesCartItems = 2,
                    Difficulty = "Easy",
                    PrepTime = "15 min",
                    Tags = new[] { "Uses 2 cart items", "Easy", "15 min" }
                },
                new RecipeCardViewModel
                {
                    Title = "Watermelon Farm Bowl",
                    Description = "Refreshing watermelon bowl with feta, mint, and a zesty lemon dressing.",
                    ImageUrl = "/images/recipes/watermelon-farm-bowl.jpg",
                    UsesCartItems = 1,
                    Difficulty = "Easy",
                    PrepTime = "10 min",
                    Tags = new[] { "Uses 1 cart item", "Easy", "10 min" }
                }
            };

            // Get actual products from database that are available
            var suggestedAddons = availableProducts
                .Where(p => p.IsForSale && p.InventoryQuantity > 0)
                .Take(5)
                .Select(p => new AddonProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Price = p.Pricing,
                    ImageUrl = !string.IsNullOrWhiteSpace(p.ImageUrl) 
                        ? p.ImageUrl 
                        : "/images/placeholder-product.jpg"
                })
                .ToList();

            return Task.FromResult(new RecipeSuggestionResult
            {
                Recipes = recipes,
                SuggestedAddons = suggestedAddons
            });
        }
    }
}