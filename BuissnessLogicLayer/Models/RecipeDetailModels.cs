using DataAccessLayer.Entities;

namespace BuissnessLogicLayer.Models
{
    public class RecipeDetailViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? TimeMinutes { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public int? Servings { get; set; }
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public IReadOnlyList<RecipeIngredientViewModel> Ingredients { get; set; } = Array.Empty<RecipeIngredientViewModel>();
        public IReadOnlyList<RecipeInstructionStepViewModel> Instructions { get; set; } = Array.Empty<RecipeInstructionStepViewModel>();
        public IReadOnlyList<ProductModel> MissingDatabaseProducts { get; set; } = Array.Empty<ProductModel>();
    }

    public class RecipeIngredientViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public bool IsInCart { get; set; }
        public bool IsAvailableInStore { get; set; }
        public int? ProductId { get; set; }
    }

    public class RecipeInstructionStepViewModel
    {
        public int StepNumber { get; set; }
        public string Instruction { get; set; } = string.Empty;
    }
}