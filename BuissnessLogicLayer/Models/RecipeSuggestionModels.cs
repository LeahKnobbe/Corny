using DataAccessLayer.Entities;

namespace BuissnessLogicLayer.Models
{
    public class RecipeCardViewModel
    {
        public int? SpoonacularRecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int UsesCartItems { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public string PrepTime { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class AddonProductViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class RecipeSuggestionResult
    {
        public IReadOnlyList<RecipeCardViewModel> Recipes { get; set; } = Array.Empty<RecipeCardViewModel>();
        public IReadOnlyList<AddonProductViewModel> SuggestedAddons { get; set; } = Array.Empty<AddonProductViewModel>();
    }

    public class CartItemInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Sizing { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}