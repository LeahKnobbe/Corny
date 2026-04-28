using BuissnessLogicLayer.Models;

namespace CORNY.Models.ViewModels
{
    public class RecipeSuggestionViewModel
    {
        public IReadOnlyList<CartItemViewModel> CartItems { get; set; } = Array.Empty<CartItemViewModel>();
        public IReadOnlyList<RecipeCardViewModel> Recipes { get; set; } = Array.Empty<RecipeCardViewModel>();
        public IReadOnlyList<AddonProductViewModel> SuggestedAddons { get; set; } = Array.Empty<AddonProductViewModel>();
        public string? SelectedFilter { get; set; }
    }
}